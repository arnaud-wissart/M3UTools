namespace M3UPlayer.Core.Models;

/// <summary>
/// Identifie le type de média représenté dans une entrée M3U.
/// </summary>
public enum MediaType
{
    /// <summary>Flux de chaîne TV en direct.</summary>
    LiveChannel,

    /// <summary>Film ou VOD.</summary>
    Movie,

    /// <summary>Série TV ou contenu épisodique.</summary>
    Series,

    /// <summary>Type non déterminé.</summary>
    Unknown
}
