using System;
using System.Collections.Generic;

namespace M3UPlayer.Core.Models;

/// <summary>
/// Représente une entrée M3U enrichie contenant métadonnées et URL de flux.
/// </summary>
/// <param name="Id">Identifiant unique extrait (par ex. tvg-id) ou généré.</param>
/// <param name="Name">Nom affiché de la piste.</param>
/// <param name="MediaType">Type de média (live, film, série, inconnu).</param>
/// <param name="CountryCode">Code pays ISO 3166-1 Alpha-2 si disponible.</param>
/// <param name="LanguageCode">Code langue ISO 639-1 si disponible.</param>
/// <param name="GroupTitle">Titre du groupe tel que défini dans la playlist.</param>
/// <param name="LogoUrl">URL du logo si présent.</param>
/// <param name="StreamUrl">URL HTTP/HTTPS du flux.</param>
/// <param name="Attributes">Attributs bruts issus des tags EXTINF ou autres.</param>
public sealed record class M3uTrack(
    string Id,
    string Name,
    MediaType MediaType,
    string? CountryCode,
    string? LanguageCode,
    string? GroupTitle,
    string? LogoUrl,
    string StreamUrl,
    IReadOnlyDictionary<string, string> Attributes)
{
    /// <summary>
    /// Attributs bruts tels que lus dans le fichier (ex. tvg-id, tvg-name, group-title, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        Attributes ?? throw new ArgumentNullException(nameof(Attributes));
}
