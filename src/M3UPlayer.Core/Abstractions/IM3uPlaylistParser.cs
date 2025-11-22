using System.IO;
using System.Threading;
using System.Threading.Tasks;
using M3UPlayer.Core.Models;

namespace M3UPlayer.Core.Abstractions;

/// <summary>
/// Parseur de playlists M3U étendues pour IPTV.
/// </summary>
public interface IM3uPlaylistParser
{
    /// <summary>
    /// Analyse un flux M3U en streaming (lecture ligne par ligne, sans charger tout le fichier en mémoire)
    /// et retourne le résultat sous forme de <see cref="ParsedPlaylist"/>.
    /// Le stream d'entrée n'est pas fermé par l'implémentation.
    /// Doit pouvoir traiter des fichiers jusqu'à 100 Mo.
    /// </summary>
    /// <param name="stream">Flux de lecture positionné au début du fichier M3U.</param>
    /// <param name="cancellationToken">Jeton d'annulation pour interrompre le parsing.</param>
    /// <returns>Une playlist parsée contenant toutes les pistes trouvées.</returns>
    Task<ParsedPlaylist> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}
