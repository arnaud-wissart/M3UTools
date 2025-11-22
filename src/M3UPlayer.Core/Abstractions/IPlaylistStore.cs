using System.Threading;
using System.Threading.Tasks;
using M3UPlayer.Core.Models;

namespace M3UPlayer.Core.Abstractions;

/// <summary>
/// Simple cache de playlists parsées pour exposer l'API sans persistance durable.
/// Placé dans le Core pour rester réutilisable par d'autres frontends sans dépendre de l'API.
/// </summary>
public interface IPlaylistStore
{
    /// <summary>
    /// Enregistre une playlist parsée et retourne l'identifiant associé.
    /// </summary>
    Task<string> SaveAsync(ParsedPlaylist playlist, CancellationToken ct = default);

    /// <summary>
    /// Récupère une playlist parsée si elle est toujours présente en mémoire.
    /// </summary>
    Task<ParsedPlaylist?> GetAsync(string playlistId, CancellationToken ct = default);
}
