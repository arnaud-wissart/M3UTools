using System.Collections.Generic;
using M3UPlayer.Core.Models;
using Xunit;

namespace M3UPlayer.Core.Tests;

/// <summary>
/// Tests simples pour vérifier la construction d'un <see cref="M3uTrack"/>.
/// </summary>
public sealed class M3uTrackTests
{
    /// <summary>
    /// Vérifie que toutes les propriétés sont correctement renseignées par le constructeur.
    /// </summary>
    [Fact]
    public void Constructor_ShouldPopulateProperties()
    {
        var attributes = new Dictionary<string, string>
        {
            ["tvg-id"] = "channel-001",
            ["group-title"] = "News"
        };

        var track = new M3uTrack(
            Id: "channel-001",
            Name: "News Channel",
            MediaType: MediaType.LiveChannel,
            CountryCode: "FR",
            LanguageCode: "fr",
            GroupTitle: "News",
            LogoUrl: "https://example.test/logo.png",
            StreamUrl: "https://example.test/stream.m3u8",
            Attributes: attributes);

        Assert.Equal("channel-001", track.Id);
        Assert.Equal("News Channel", track.Name);
        Assert.Equal(MediaType.LiveChannel, track.MediaType);
        Assert.Equal("FR", track.CountryCode);
        Assert.Equal("fr", track.LanguageCode);
        Assert.Equal("News", track.GroupTitle);
        Assert.Equal("https://example.test/logo.png", track.LogoUrl);
        Assert.Equal("https://example.test/stream.m3u8", track.StreamUrl);
        Assert.Same(attributes, track.Attributes);
    }
}
