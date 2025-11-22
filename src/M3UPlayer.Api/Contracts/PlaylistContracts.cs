using System.Collections.Generic;

namespace M3UPlayer.Api.Contracts;

/// <summary>
/// Réponse retournée après le chargement ou la récupération d'une playlist.
/// </summary>
/// <param name="PlaylistId">Identifiant généré et utilisé pour récupérer la playlist.</param>
/// <param name="Countries">Synthèse des pays regroupés; seules les chaînes de type <c>LiveChannel</c> sont comptées.</param>
public sealed record PlaylistSummaryResponse(string PlaylistId, IReadOnlyList<CountrySummaryDto> Countries);

/// <summary>
/// Vue synthétique d'un groupe de chaînes par pays.
/// </summary>
/// <param name="Code">Code pays (ou valeur <c>UNSPECIFIED</c> lorsqu'absent).</param>
/// <param name="Language">Langue majoritaire détectée.</param>
/// <param name="ChannelCount">Nombre de chaînes dans le groupe (uniquement les flux live).</param>
public sealed record CountrySummaryDto(string Code, string? Language, int ChannelCount);

/// <summary>
/// Données retournées pour une chaîne individuelle.
/// </summary>
/// <param name="Id">Identifiant de la chaîne.</param>
/// <param name="Name">Nom affiché.</param>
/// <param name="CountryCode">Code pays ISO si disponible.</param>
/// <param name="LanguageCode">Code langue ISO si disponible.</param>
/// <param name="LogoUrl">URL du logo.</param>
/// <param name="StreamUrl">URL du flux streamable.</param>
/// <param name="MediaType">Type de média au format texte (LiveChannel, Movie...).</param>
public sealed record ChannelDto(
    string Id,
    string Name,
    string? CountryCode,
    string? LanguageCode,
    string? LogoUrl,
    string StreamUrl,
    string MediaType);

/// <summary>
/// Requête pour charger une playlist depuis une URL distante.
/// </summary>
/// <param name="Url">URL HTTP/HTTPS vers le fichier M3U.</param>
public sealed record PlaylistFromUrlRequest(string Url);
