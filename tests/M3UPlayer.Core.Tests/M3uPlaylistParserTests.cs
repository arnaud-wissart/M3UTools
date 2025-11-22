using System.IO;
using System.Text;
using System.Threading.Tasks;
using M3UPlayer.Core.Models;
using M3UPlayer.Core.Parsing;
using Xunit;

namespace M3UPlayer.Core.Tests;

/// <summary>
/// Tests de parsing pour <see cref="M3uPlaylistParser"/>.
/// </summary>
public sealed class M3uPlaylistParserTests
{
    /// <summary>
    /// Vérifie le parsing complet d'une entrée simple avec attributs principaux.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ShouldParseBasicPlaylist()
    {
        const string content = """
#EXTM3U
#EXTINF:-1 tvg-id="canal" tvg-name="Canal+" tvg-logo="https://logo.test/canal.png" tvg-country="fr" tvg-language="fr",FR | Canal+
http://stream.test/canal.m3u8
""";

        await using var stream = CreateStream(content);
        var parser = new M3uPlaylistParser();

        var result = await parser.ParseAsync(stream);

        var track = Assert.Single(result.Tracks);
        Assert.Equal("canal", track.Id);
        Assert.Equal("Canal+", track.Name);
        Assert.Equal(MediaType.LiveChannel, track.MediaType);
        Assert.Equal("FR", track.CountryCode);
        Assert.Equal("fr", track.LanguageCode);
        Assert.Equal("https://logo.test/canal.png", track.LogoUrl);
        Assert.Equal("http://stream.test/canal.m3u8", track.StreamUrl);
        Assert.Equal("fr", track.Attributes["tvg-country"]);
        Assert.Equal("Canal+", track.Attributes["tvg-name"]);
    }

    /// <summary>
    /// Vérifie l'extraction des attributs et de l'URL.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ShouldExtractAttributesAndUrls()
    {
        const string content = """
#EXTM3U
#EXTINF:-1 tvg-id="movie-1" group-title="VOD Movies" custom="value",Movie One
http://stream.test/movie1.m3u8
""";

        await using var stream = CreateStream(content);
        var parser = new M3uPlaylistParser();

        var result = await parser.ParseAsync(stream);
        var track = Assert.Single(result.Tracks);

        Assert.Equal("movie-1", track.Id);
        Assert.Equal("Movie One", track.Name);
        Assert.Equal(MediaType.Movie, track.MediaType);
        Assert.Equal("value", track.Attributes["custom"]);
        Assert.Equal("VOD Movies", track.GroupTitle);
    }

    /// <summary>
    /// Vérifie la détection du type Série à partir du group-title.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ShouldDetectSeriesMediaType()
    {
        const string content = """
#EXTM3U
#EXTINF:-1 group-title="Séries FR",Séries Show
http://stream.test/series.m3u8
""";

        await using var stream = CreateStream(content);
        var parser = new M3uPlaylistParser();

        var result = await parser.ParseAsync(stream);
        var track = Assert.Single(result.Tracks);

        Assert.Equal(MediaType.Series, track.MediaType);
    }

    /// <summary>
    /// Vérifie la détection du pays/langue via préfixe dans le nom.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ShouldUsePrefixHeuristicsForCountryAndLanguage()
    {
        const string content = """
#EXTM3U
#EXTINF:-1 ,[FR] Film Club
http://stream.test/filmclub.m3u8
""";

        await using var stream = CreateStream(content);
        var parser = new M3uPlaylistParser();

        var result = await parser.ParseAsync(stream);
        var track = Assert.Single(result.Tracks);

        Assert.Equal("FR", track.CountryCode);
        Assert.Equal("fr", track.LanguageCode);
        Assert.Equal("Film Club", track.Name);
    }

    /// <summary>
    /// Vérifie que la langue définie dans les attributs prime sur la détection par préfixe.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ShouldFavorAttributeLanguageOverPrefix()
    {
        const string content = """
#EXTM3U
#EXTINF:-1 tvg-language="en",FR - English News
http://stream.test/news.m3u8
""";

        await using var stream = CreateStream(content);
        var parser = new M3uPlaylistParser();

        var result = await parser.ParseAsync(stream);
        var track = Assert.Single(result.Tracks);

        Assert.Equal("en", track.LanguageCode);
        Assert.Equal("FR", track.CountryCode);
        Assert.Equal("English News", track.Name);
    }

    private static MemoryStream CreateStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));
}
