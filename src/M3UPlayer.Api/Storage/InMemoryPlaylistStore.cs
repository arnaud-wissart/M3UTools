using System;
using System.Threading;
using System.Threading.Tasks;
using M3UPlayer.Core.Abstractions;
using M3UPlayer.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace M3UPlayer.Api.Storage;

/// <summary>
/// Stockage en mémoire avec expiration pour éviter la croissance non contrôlée.
/// IMemoryCache gère la concurrence et l'expiration, sans copie inutile du contenu parsé.
/// </summary>
public sealed class InMemoryPlaylistStore : IPlaylistStore
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ILogger<InMemoryPlaylistStore> _logger;

    /// <summary>
    /// Initialise le store mémoire avec expiration.
    /// </summary>
    /// <param name="cache">Cache partagé pour stocker les playlists.</param>
    /// <param name="logger">Logger pour la visibilité des évictions.</param>
    public InMemoryPlaylistStore(IMemoryCache cache, ILogger<InMemoryPlaylistStore> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        _cacheEntryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
        {
            EvictionCallback = (key, _, reason, _) =>
            {
                if (key is string playlistId && reason != EvictionReason.Removed)
                {
                    _logger.LogDebug("Playlist {PlaylistId} evicted from memory ({Reason}).", playlistId, reason);
                }
            }
        });
    }

    /// <inheritdoc />
    public Task<string> SaveAsync(ParsedPlaylist playlist, CancellationToken ct = default)
    {
        if (playlist is null)
        {
            throw new ArgumentNullException(nameof(playlist));
        }

        ct.ThrowIfCancellationRequested();

        var playlistId = Guid.NewGuid().ToString("N");
        var snapshot = playlist with { PlaylistId = playlistId };

        _cache.Set(playlistId, snapshot, _cacheEntryOptions);

        return Task.FromResult(playlistId);
    }

    /// <inheritdoc />
    public Task<ParsedPlaylist?> GetAsync(string playlistId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentException("Playlist identifier is required.", nameof(playlistId));
        }

        ct.ThrowIfCancellationRequested();

        if (_cache.TryGetValue<ParsedPlaylist>(playlistId, out var playlist))
        {
            return Task.FromResult<ParsedPlaylist?>(playlist);
        }

        return Task.FromResult<ParsedPlaylist?>(null);
    }
}
