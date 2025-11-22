using System;
using System.Collections.Generic;
using System.Linq;
using M3UPlayer.Core.Models;

namespace M3UPlayer.Core.Services;

/// <summary>
/// Regroupe des pistes M3U par pays ou langue sans modifier les données sources.
/// </summary>
public static class ChannelGroupingService
{
    /// <summary>
    /// Clé utilisée pour les pistes sans pays ou langue renseignés.
    /// </summary>
    public const string UnspecifiedKey = "UNSPECIFIED";

    /// <summary>
    /// Regroupe les pistes par code pays (ISO 3166-1 alpha-2 ou <see cref="UnspecifiedKey"/>).
    /// </summary>
    /// <param name="playlist">Playlist déjà parsée.</param>
    /// <returns>Liste de groupes par pays.</returns>
    public static IReadOnlyList<ChannelGroup> GroupByCountry(ParsedPlaylist playlist)
    {
        return GroupByKey(playlist, track => track.CountryCode);
    }

    /// <summary>
    /// Regroupe les pistes par code langue (ISO 639-1 ou <see cref="UnspecifiedKey"/>).
    /// </summary>
    /// <param name="playlist">Playlist déjà parsée.</param>
    /// <returns>Liste de groupes par langue.</returns>
    public static IReadOnlyList<ChannelGroup> GroupByLanguage(ParsedPlaylist playlist)
    {
        return GroupByKey(playlist, track => track.LanguageCode);
    }

    private static IReadOnlyList<ChannelGroup> GroupByKey(ParsedPlaylist playlist, Func<M3uTrack, string?> keySelector)
    {
        if (playlist is null)
        {
            throw new ArgumentNullException(nameof(playlist));
        }

        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        var groups = new Dictionary<string, List<M3uTrack>>(StringComparer.OrdinalIgnoreCase);

        foreach (var track in playlist.Tracks)
        {
            if (track is null)
            {
                continue;
            }

            var key = keySelector(track);
            key = string.IsNullOrWhiteSpace(key) ? UnspecifiedKey : key!;

            if (!groups.TryGetValue(key, out var bucket))
            {
                bucket = new List<M3uTrack>();
                groups[key] = bucket;
            }

            bucket.Add(track);
        }

        return groups
            .Select(pair => new ChannelGroup(pair.Key, pair.Value.AsReadOnly()))
            .ToList();
    }
}

/// <summary>
/// Représente un groupe logique de chaînes, associé à une clé (pays ou langue).
/// </summary>
/// <param name="Key">Clé de regroupement (code pays, code langue ou <see cref="ChannelGroupingService.UnspecifiedKey"/>).</param>
/// <param name="Tracks">Pistes appartenant au groupe.</param>
public sealed record class ChannelGroup(string Key, IReadOnlyList<M3uTrack> Tracks)
{
    /// <summary>
    /// Pistes du groupe; la liste n'est jamais nulle.
    /// </summary>
    public IReadOnlyList<M3uTrack> Tracks { get; init; } = Tracks ?? throw new ArgumentNullException(nameof(Tracks));
}
