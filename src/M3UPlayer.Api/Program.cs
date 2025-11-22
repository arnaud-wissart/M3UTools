using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using M3UPlayer.Api.Contracts;
using M3UPlayer.Api.Storage;
using M3UPlayer.Core.Abstractions;
using M3UPlayer.Core.DependencyInjection;
using M3UPlayer.Core.Models;
using M3UPlayer.Core.Services;
using M3UPlayer.ServiceDefaults;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.AddM3UPlayerServiceDefaults();

builder.Services.AddM3uCore();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.Configure<FormOptions>(options =>
{
    // Autoriser des uploads volumineux (100 Mo) en multipart.
    options.MultipartBodyLengthLimit = 200 * 1024L * 1024L;
});
builder.Services.AddSingleton<IPlaylistStore, InMemoryPlaylistStore>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapM3UPlayerDefaultEndpoints();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Json(new { status = "OK", service = "M3UPlayer.Api" }));

api.MapPost("/playlists/from-file", async Task<IResult> (
    IFormFile file,
    IM3uPlaylistParser parser,
    IPlaylistStore store,
    CancellationToken ct) =>
{
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("A non-empty M3U file is required.");
    }

    await using var stream = file.OpenReadStream();
    var parsed = await parser.ParseAsync(stream, ct);
    var playlistId = await store.SaveAsync(parsed, ct);

    var response = BuildPlaylistSummaryResponse(playlistId, parsed);
    return Results.Ok(response);
}).DisableAntiforgery();

api.MapPost("/playlists/from-url", async Task<IResult> (
    PlaylistFromUrlRequest request,
    IHttpClientFactory httpClientFactory,
    IM3uPlaylistParser parser,
    IPlaylistStore store,
    CancellationToken ct) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest("The 'url' field is required.");
    }

    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
    {
        return Results.BadRequest("The 'url' field must be a valid absolute URI.");
    }

    var httpClient = httpClientFactory.CreateClient();
    using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem($"Failed to download playlist: {(int)response.StatusCode} {response.ReasonPhrase}", statusCode: (int)response.StatusCode);
    }

    await using var stream = await response.Content.ReadAsStreamAsync(ct);
    var parsed = await parser.ParseAsync(stream, ct);
    var playlistId = await store.SaveAsync(parsed, ct);

    var payload = BuildPlaylistSummaryResponse(playlistId, parsed);
    return Results.Ok(payload);
}).DisableAntiforgery();

api.MapGet("/playlists/{playlistId}/countries", async Task<IResult> (
    string playlistId,
    IPlaylistStore store,
    CancellationToken ct) =>
{
    var playlist = await store.GetAsync(playlistId, ct);
    if (playlist is null)
    {
        return Results.NotFound();
    }

    var response = BuildPlaylistSummaryResponse(playlistId, playlist);
    return Results.Ok(response);
});

api.MapGet("/playlists/{playlistId}/countries/{code}/channels", async Task<IResult> (
    string playlistId,
    string code,
    IPlaylistStore store,
    CancellationToken ct) =>
{
    var playlist = await store.GetAsync(playlistId, ct);
    if (playlist is null)
    {
        return Results.NotFound();
    }

    var channels = BuildChannelsForCountry(playlist, code);
    return Results.Ok(channels);
});

api.MapGet("/playlists/{playlistId}/vod", async Task<IResult> (
    string playlistId,
    IPlaylistStore store,
    CancellationToken ct) =>
{
    var playlist = await store.GetAsync(playlistId, ct);
    if (playlist is null)
    {
        return Results.NotFound();
    }

    // La VOD est renvoyée telle quelle (liste plate) pour simplifier le front;
    // seuls les contenus Movie ou Series sont exposés ici.
    var vodItems = playlist.Tracks
        .Where(track => track is not null &&
                        (track.MediaType == MediaType.Movie || track.MediaType == MediaType.Series))
        .Select(track => new ChannelDto(
            track.Id,
            track.Name,
            track.CountryCode,
            track.LanguageCode,
            track.LogoUrl,
            track.StreamUrl,
            track.MediaType.ToString()))
        .ToList();

    return Results.Ok(vodItems);
});

app.Run();

static PlaylistSummaryResponse BuildPlaylistSummaryResponse(string playlistId, ParsedPlaylist playlist)
{
    var countries = BuildCountrySummaries(playlist);
    return new PlaylistSummaryResponse(playlistId, countries);
}

static IReadOnlyList<CountrySummaryDto> BuildCountrySummaries(ParsedPlaylist playlist)
{
    // Les pays exposés au front principal sont calculés uniquement à partir des chaînes live
    // pour fournir un premier rendu rapide; les films/séries sont servis via l'endpoint VOD.
    var grouped = ChannelGroupingService.GroupByCountry(playlist, MediaType.LiveChannel);

    if (grouped.Count == 0)
    {
        return Array.Empty<CountrySummaryDto>();
    }

    return grouped
        .Select(group => new CountrySummaryDto(
            group.Key,
            ResolveRepresentativeLanguage(group.Tracks),
            group.Tracks.Count))
        .ToList();
}

static string? ResolveRepresentativeLanguage(IEnumerable<M3uTrack> tracks)
{
    return tracks
        .Select(track => track.LanguageCode)
        .Where(language => !string.IsNullOrWhiteSpace(language))
        .GroupBy(language => language!, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.Key)
        .FirstOrDefault();
}

static IReadOnlyList<ChannelDto> BuildChannelsForCountry(ParsedPlaylist playlist, string code)
{
    var normalizedCode = string.IsNullOrWhiteSpace(code)
        ? ChannelGroupingService.UnspecifiedKey
        : code;

    // On réutilise le regroupement Live pour garantir la même logique que /countries.
    var group = ChannelGroupingService.GroupByCountry(playlist, MediaType.LiveChannel)
        .FirstOrDefault(g => string.Equals(g.Key, normalizedCode, StringComparison.OrdinalIgnoreCase));

    if (group is null)
    {
        return Array.Empty<ChannelDto>();
    }

    return group.Tracks
        .Select(track => new ChannelDto(
            track.Id,
            track.Name,
            track.CountryCode,
            track.LanguageCode,
            track.LogoUrl,
            track.StreamUrl,
            track.MediaType.ToString()))
        .ToList();
}

// Rendu public pour permettre aux tests d'héberger l'application via WebApplicationFactory.
/// <summary>
/// Point d'entrée exposé pour l'hébergement de l'API en intégration.
/// </summary>
public partial class Program { }
