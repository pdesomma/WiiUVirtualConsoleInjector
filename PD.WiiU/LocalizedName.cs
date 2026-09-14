namespace PD.WiiU;

/// <summary>
/// How a title presents itself in one language.
/// </summary>
/// <param name="ShortName">Shown where space is tight, e.g. under the icon on the menu.</param>
/// <param name="LongName">Shown where space allows, e.g. the banner. May contain a newline to split the name over two lines.</param>
public sealed record LocalizedName(string ShortName, string LongName)
{
    /// <summary>A name that reads the same in short and long form.</summary>
    public LocalizedName(string name) : this(name, name)
    {
    }

    /// <summary>Uses <paramref name="name"/> for every language the Wii U supports.</summary>
    public static IReadOnlyDictionary<Language, LocalizedName> ForAllLanguages(LocalizedName name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        var names = new Dictionary<Language, LocalizedName>();
        foreach (Language language in Enum.GetValues(typeof(Language)))
            names[language] = name;
        return names;
    }
}
