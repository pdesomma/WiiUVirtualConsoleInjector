namespace PD.WiiU;

/// <summary>
/// How a title presents itself in one language.
/// </summary>
/// <param name="ShortName">Shown where space is tight, e.g. under the icon on the menu.</param>
/// <param name="LongName">Shown where space allows, e.g. the banner. May contain a newline to split the name over two lines.</param>
public sealed record LocalizedName(string ShortName, string LongName)
{
    /// <summary>
    /// Creates a new instance of the <see cref="LocalizedName"/> record that reads the same in short and long form.
    /// </summary>
    /// <param name="name">The name to use for both forms.</param>
    public LocalizedName(string name) : this(name, name)
    {
    }

    /// <summary>
    /// Uses <paramref name="name"/> for every language the Wii U supports.
    /// </summary>
    /// <param name="name">The name to use in every language.</param>
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
