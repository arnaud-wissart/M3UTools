using Aspire.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

namespace M3UPlayer.ServiceDefaults;

/// <summary>
/// Extensions pour appliquer la configuration par défaut des services.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Enregistre la configuration commune (logs, health checks, résilience HTTP) pour un hôte classique.
    /// </summary>
    public static IHostApplicationBuilder AddM3UPlayerServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddLogging();
        builder.Services.AddHealthChecks();
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        return builder;
    }

    /// <summary>
    /// Enregistre la configuration commune pour l’AppHost Aspire.
    /// </summary>
    public static IDistributedApplicationBuilder AddM3UPlayerServiceDefaults(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLogging();
        builder.Services.AddHealthChecks();
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        return builder;
    }

    /// <summary>
    /// Expose les points d’extrémité de supervision.
    /// </summary>
    public static WebApplication MapM3UPlayerDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/healthz");
        return app;
    }
}
