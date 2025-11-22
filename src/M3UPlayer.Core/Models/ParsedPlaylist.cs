using System;
using System.Collections.Generic;

namespace M3UPlayer.Core.Models;

/// <summary>
/// Représente le résultat complet du parsing d'un fichier M3U.
/// </summary>
/// <param name="PlaylistId">Identifiant de la playlist (nom de fichier, hash, etc.).</param>
/// <param name="Tracks">Liste immuable des pistes parsées.</param>
public sealed record class ParsedPlaylist(string PlaylistId, IReadOnlyList<M3uTrack> Tracks)
{
    /// <summary>
    /// Pistes M3U parsées; la liste n'est jamais nulle.
    /// </summary>
    public IReadOnlyList<M3uTrack> Tracks { get; init; } = Tracks ?? throw new ArgumentNullException(nameof(Tracks));
}
