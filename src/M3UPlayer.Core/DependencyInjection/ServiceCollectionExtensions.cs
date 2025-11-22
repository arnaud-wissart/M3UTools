using System;
using M3UPlayer.Core.Abstractions;
using M3UPlayer.Core.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace M3UPlayer.Core.DependencyInjection;

/// <summary>
/// Extensions d'enregistrement pour les composants du domaine Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre les services Core nécessaires pour le parsing et la manipulation de playlists M3U.
    /// </summary>
    public static IServiceCollection AddM3uCore(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Parser sans état => singleton suffisant et évite des allocations inutiles.
        services.AddSingleton<IM3uPlaylistParser, M3uPlaylistParser>();

        return services;
    }
}
