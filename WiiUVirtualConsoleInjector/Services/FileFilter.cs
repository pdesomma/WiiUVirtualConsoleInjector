namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// One entry of a file picker's type list.
/// </summary>
/// <param name="Name">Label shown to the user.</param>
/// <param name="Patterns">Globs such as *.nes.</param>
public sealed record FileFilter(string Name, params string[] Patterns);
