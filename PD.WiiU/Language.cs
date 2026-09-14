namespace PD.WiiU;

/// <summary>
/// The twelve system languages the Wii U can display a title's name in.
/// </summary>
public enum Language
{
    Japanese,
    English,
    French,
    German,
    Italian,
    Spanish,
    SimplifiedChinese,
    Korean,
    Dutch,
    Portuguese,
    Russian,
    TraditionalChinese,
}

/// <summary>
/// Wii U specifics of each <see cref="Language"/>.
/// </summary>
public static class LanguageExtensions
{
    /// <summary>
    /// The short code the Wii U uses to tag per-language fields, e.g. <c>en</c> or <c>zhs</c>.
    /// </summary>
    /// <param name="language">The language to get the code for.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="language"/> is not a defined <see cref="Language"/>.</exception>
    public static string Code(this Language language) => language switch
    {
        Language.Japanese => "ja",
        Language.English => "en",
        Language.French => "fr",
        Language.German => "de",
        Language.Italian => "it",
        Language.Spanish => "es",
        Language.SimplifiedChinese => "zhs",
        Language.Korean => "ko",
        Language.Dutch => "nl",
        Language.Portuguese => "pt",
        Language.Russian => "ru",
        Language.TraditionalChinese => "zht",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown Wii U language."),
    };
}
