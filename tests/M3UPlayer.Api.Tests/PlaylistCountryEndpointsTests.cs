using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using M3UPlayer.Api.Contracts;
using M3UPlayer.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace M3UPlayer.Api.Tests;

/// <summary>
/// Tests d'intégration des endpoints pays/chaînes pour garantir que seuls les flux live
/// sont comptés dans le regroupement principal.
/// </summary>
public sealed class PlaylistCountryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string PlaylistWithVod = """
#EXTM3U
#EXTINF:-1 tvg-id="live-1" tvg-country="FR",Live News
http://test/live1.m3u8
#EXTINF:-1 tvg-id="vod-1" tvg-country="FR" group-title="VOD Movies",Movie One
http://test/movie1.m3u8
#EXTINF:-1 tvg-id="series-1" tvg-country="FR" group-title="Series FR",Series One
http://test/series1.m3u8
#EXTINF:-1 tvg-id="live-2" tvg-country="FR",Live Sport
http://test/live2.m3u8
""";

    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initialise la factory de l'API pour les tests d'intégration.
    /// </summary>
    public PlaylistCountryEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Vérifie que l'agrégat /countries ne compte que les chaînes live (à la création et à la récupération).
    /// </summary>
    [Fact]
    public async Task CountriesEndpoint_ShouldCountOnlyLiveChannels()
    {
        var client = _factory.CreateClient();
        var summary = await UploadPlaylistAsync(client);

        var postedCountry = Assert.Single(summary.Countries);
        Assert.Equal(2, postedCountry.ChannelCount);

        var fetched = await client.GetFromJsonAsync<PlaylistSummaryResponse>($"/api/playlists/{summary.PlaylistId}/countries");
        Assert.NotNull(fetched);

        var country = Assert.Single(fetched!.Countries);
        Assert.Equal(2, country.ChannelCount);
    }

    /// <summary>
    /// Vérifie que le détail d'un pays retourne uniquement des chaînes live.
    /// </summary>
    [Fact]
    public async Task ChannelsByCountry_ShouldReturnOnlyLiveChannels()
    {
        var client = _factory.CreateClient();
        var summary = await UploadPlaylistAsync(client);

        var channels = await client.GetFromJsonAsync<List<ChannelDto>>($"/api/playlists/{summary.PlaylistId}/countries/FR/channels");
        Assert.NotNull(channels);
        Assert.Equal(2, channels!.Count);
        Assert.All(channels!, channel =>
        {
            Assert.Equal(MediaType.LiveChannel.ToString(), channel.MediaType);
        });
    }

    /// <summary>
    /// Vérifie que l'endpoint VOD renvoie bien uniquement les films/séries.
    /// </summary>
    [Fact]
    public async Task VodEndpoint_ShouldExposeMoviesAndSeriesOnly()
    {
        var client = _factory.CreateClient();
        var summary = await UploadPlaylistAsync(client);

        var vodItems = await client.GetFromJsonAsync<List<ChannelDto>>($"/api/playlists/{summary.PlaylistId}/vod");
        Assert.NotNull(vodItems);

        Assert.Equal(2, vodItems!.Count);
        Assert.All(vodItems!, item =>
        {
            Assert.Contains(item.MediaType, new[] { MediaType.Movie.ToString(), MediaType.Series.ToString() });
        });
    }

    private static async Task<PlaylistSummaryResponse> UploadPlaylistAsync(HttpClient client)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(PlaylistWithVod)));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "playlist.m3u");

        var response = await client.PostAsync("/api/playlists/from-file", content);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase} - {payload}");

        var summary = await response.Content.ReadFromJsonAsync<PlaylistSummaryResponse>();
        Assert.NotNull(summary);

        return summary!;
    }
}
