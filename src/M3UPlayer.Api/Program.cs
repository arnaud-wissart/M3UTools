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
});

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
});

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

app.Run();

static PlaylistSummaryResponse BuildPlaylistSummaryResponse(string playlistId, ParsedPlaylist playlist)
{
    var countries = BuildCountrySummaries(playlist);
    return new PlaylistSummaryResponse(playlistId, countries);
}

static IReadOnlyList<CountrySummaryDto> BuildCountrySummaries(ParsedPlaylist playlist)
{
    var liveTracks = playlist.Tracks
        .Where(track => track is not null && track.MediaType == MediaType.LiveChannel)
        .ToList();

    if (liveTracks.Count == 0)
    {
        return Array.Empty<CountrySummaryDto>();
    }

    var grouped = ChannelGroupingService.GroupByCountry(new ParsedPlaylist(playlist.PlaylistId, liveTracks));

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

    return playlist.Tracks
        .Where(track => track is not null && track.MediaType == MediaType.LiveChannel)
        .Where(track =>
        {
            // On filtre par code pays pour rester cohérent avec le regroupement principal.
            var country = string.IsNullOrWhiteSpace(track.CountryCode)
                ? ChannelGroupingService.UnspecifiedKey
                : track.CountryCode;
            return string.Equals(country, normalizedCode, StringComparison.OrdinalIgnoreCase);
        })
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
