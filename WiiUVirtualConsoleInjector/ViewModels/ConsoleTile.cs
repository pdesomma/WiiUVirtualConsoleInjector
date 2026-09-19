using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Assets;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One tile on the console step: a console, or a company whose consoles open when it is clicked.
/// </summary>
public sealed class ConsoleTile
{
    /// <summary>
    /// Creates a tile for one console.
    /// </summary>
    /// <param name="console">The console.</param>
    public ConsoleTile(SourceConsole console)
    {
        Console = console;
        Consoles = new[] { console };
        Label = ConsoleIcons.DisplayName(console);
        Caption = ConsoleIcons.Caption(console);
        IconName = console.ToString();
    }

    /// <summary>
    /// Creates a tile for a company.
    /// </summary>
    /// <param name="name">Company name; also its logo's file name.</param>
    /// <param name="consoles">Its consoles, in the order shown.</param>
    public ConsoleTile(string name, IReadOnlyList<SourceConsole> consoles)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (consoles is null)
            throw new ArgumentNullException(nameof(consoles));
        if (consoles.Count == 0)
            throw new ArgumentException("A company needs at least one console.", nameof(consoles));

        Label = name;
        Consoles = consoles;
        IconName = name;
    }

    /// <summary>
    /// Note under the label, or null.
    /// </summary>
    public string? Caption { get; }
    /// <summary>
    /// The console, or null for a company.
    /// </summary>
    public SourceConsole? Console { get; }
    /// <summary>
    /// The consoles the tile stands for: one for a console, several for a company.
    /// </summary>
    public IReadOnlyList<SourceConsole> Consoles { get; }
    /// <summary>
    /// File name (without extension) of the logo under Assets/Consoles.
    /// </summary>
    public string IconName { get; }
    /// <summary>
    /// True for a company.
    /// </summary>
    public bool IsGroup => Console is null;
    /// <summary>
    /// Text under the tile.
    /// </summary>
    public string Label { get; }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
