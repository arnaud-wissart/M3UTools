using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using M3UPlayer.Core.Abstractions;
using M3UPlayer.Core.Models;

namespace M3UPlayer.Core.Parsing;

/// <summary>
/// Parseur M3U qui lit en streaming afin d'éviter de charger des fichiers volumineux en mémoire.
/// </summary>
public sealed class M3uPlaylistParser : IM3uPlaylistParser
{
    private const string ExtInfPrefix = "#EXTINF:";
    private const string DefaultPlaylistId = "default";

    private static readonly Regex AttributeRegex = new(@"(?<key>[A-Za-z0-9._:-]+)\s*=\s*""(?<value>[^""]*)""", RegexOptions.Compiled);

    // Préfixes facilement extensibles pour détecter le pays/la langue dans le nom.
    private static readonly IReadOnlyDictionary<string, (string Country, string? Language)> CountryPrefixes =
        new Dictionary<string, (string Country, string? Language)>(StringComparer.OrdinalIgnoreCase)
        {
            ["FR"] = ("FR", "fr")
        };

    /// <inheritdoc />
    public async Task<ParsedPlaylist> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var tracks = new List<M3uTrack>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);

        ExtInfMetadata? pendingExtInf = null;

        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith(ExtInfPrefix, StringComparison.OrdinalIgnoreCase))
            {
                pendingExtInf = ParseExtInfLine(trimmed);
                continue;
            }

            if (pendingExtInf is not null && !trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                var track = BuildTrack(pendingExtInf, trimmed);
                tracks.Add(track);
                pendingExtInf = null;
            }
        }

        return new ParsedPlaylist(DefaultPlaylistId, tracks.AsReadOnly());
    }

    private static ExtInfMetadata ParseExtInfLine(string line)
    {
        var content = line.Substring(ExtInfPrefix.Length);
        var (metadataText, displayName) = SplitExtInfContent(content);

        var attributes = ParseAttributes(metadataText);

        return new ExtInfMetadata(attributes, displayName);
    }

    private static (string metadata, string? displayName) SplitExtInfContent(string content)
    {
        var inQuotes = false;
        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                var metadata = content[..i];
                var name = content[(i + 1)..].Trim();
                return (metadata, name);
            }
        }

        return (content, null);
    }

    private static Dictionary<string, string> ParseAttributes(string metadata)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AttributeRegex.Matches(metadata))
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;

            if (!string.IsNullOrEmpty(key))
            {
                attributes[key] = value;
            }
        }

        return attributes;
    }

    private static M3uTrack BuildTrack(ExtInfMetadata extInf, string streamUrl)
    {
        var attributesCopy = new Dictionary<string, string>(extInf.Attributes, StringComparer.OrdinalIgnoreCase);

        var groupTitle = TryGetValue(extInf.Attributes, "group-title");
        var logoUrl = TryGetValue(extInf.Attributes, "tvg-logo");

        var mediaType = InferMediaType(groupTitle);

        var (name, prefixCountry, prefixLanguage) = DetermineName(extInf);

        var country = ResolveCountry(extInf.Attributes, prefixCountry);
        var language = ResolveLanguage(extInf.Attributes, prefixLanguage);

        var id = ResolveIdentifier(extInf.Attributes, name, streamUrl);

        return new M3uTrack(
            Id: id,
            Name: name,
            MediaType: mediaType,
            CountryCode: country,
            LanguageCode: language,
            GroupTitle: groupTitle,
            LogoUrl: logoUrl,
            StreamUrl: streamUrl,
            Attributes: new ReadOnlyDictionary<string, string>(attributesCopy));
    }

    private static string ResolveIdentifier(IReadOnlyDictionary<string, string> attributes, string name, string streamUrl)
    {
        if (attributes.TryGetValue("tvg-id", out var tvgId) && !string.IsNullOrWhiteSpace(tvgId))
        {
            return tvgId;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return streamUrl;
    }

    private static MediaType InferMediaType(string? groupTitle)
    {
        if (string.IsNullOrWhiteSpace(groupTitle))
        {
            return MediaType.LiveChannel;
        }

        if (Contains(groupTitle, "Series") || Contains(groupTitle, "Séries"))
        {
            return MediaType.Series;
        }

        if (Contains(groupTitle, "VOD") || Contains(groupTitle, "Movies") || Contains(groupTitle, "Films"))
        {
            return MediaType.Movie;
        }

        return MediaType.LiveChannel;
    }

    private static (string Name, string? Country, string? Language) DetermineName(ExtInfMetadata extInf)
    {
        string rawName;
        if (extInf.Attributes.TryGetValue("tvg-name", out var tvgName) && !string.IsNullOrWhiteSpace(tvgName))
        {
            rawName = tvgName;
        }
        else
        {
            rawName = extInf.DisplayName ?? string.Empty;
        }

        rawName = rawName.Trim();

        var cleanedName = RemoveCountryPrefix(rawName, out var country, out var language);

        return (cleanedName, country, language);
    }

    private static string? ResolveCountry(IReadOnlyDictionary<string, string> attributes, string? prefixCountry)
    {
        if (attributes.TryGetValue("tvg-country", out var country) && !string.IsNullOrWhiteSpace(country))
        {
            return country.Trim().ToUpperInvariant();
        }

        return prefixCountry;
    }

    private static string? ResolveLanguage(IReadOnlyDictionary<string, string> attributes, string? prefixLanguage)
    {
        if (attributes.TryGetValue("tvg-language", out var language) && !string.IsNullOrWhiteSpace(language))
        {
            return language.Trim().ToLowerInvariant();
        }

        return prefixLanguage;
    }

    private static string RemoveCountryPrefix(string name, out string? country, out string? language)
    {
        country = null;
        language = null;

        var trimmed = name.TrimStart();

        foreach (var prefix in CountryPrefixes)
        {
            if (TryMatchPrefix(trimmed, prefix.Key, out var remainder))
            {
                country = prefix.Value.Country;
                language = prefix.Value.Language;
                return remainder.Trim();
            }
        }

        return name.Trim();
    }

    private static bool TryMatchPrefix(string value, string token, out string remainder)
    {
        remainder = value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var span = value.AsSpan().TrimStart();

        if (span.Length >= token.Length + 2 &&
            span[0] == '[' &&
            span[token.Length + 1] == ']' &&
            span.Slice(1, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase))
        {
            remainder = span.Slice(token.Length + 2).ToString().TrimStart();
            return true;
        }

        if (!span.StartsWith(token, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var afterToken = span.Slice(token.Length);
        if (afterToken.Length == 0)
        {
            return false;
        }

        var separatorCount = 0;
        while (separatorCount < afterToken.Length &&
               (afterToken[separatorCount] == ' ' ||
                afterToken[separatorCount] == '|' ||
                afterToken[separatorCount] == '-' ||
                afterToken[separatorCount] == ':'))
        {
            separatorCount++;
        }

        if (separatorCount == 0)
        {
            return false;
        }

        remainder = afterToken.Slice(separatorCount).ToString().TrimStart();
        return true;
    }

    private static string? TryGetValue(IReadOnlyDictionary<string, string> attributes, string key)
    {
        if (attributes.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    private static bool Contains(string? text, string value) =>
        text?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

    private sealed record ExtInfMetadata(IReadOnlyDictionary<string, string> Attributes, string? DisplayName);
}
