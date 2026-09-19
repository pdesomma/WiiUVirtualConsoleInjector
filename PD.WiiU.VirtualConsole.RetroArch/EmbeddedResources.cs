namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// Opens the cores and template files compiled into this assembly.
/// </summary>
internal static class EmbeddedResources
{
    /// <summary>
    /// Opens a resource by its logical name.
    /// </summary>
    /// <param name="name">Logical name, e.g. cores/genesis/picodrive_libretro.rpx.</param>
    /// <exception cref="InvalidOperationException">Not compiled in.</exception>
    public static Stream Open(string name) =>
        typeof(EmbeddedResources).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Embedded {name} is missing.");

    /// <summary>
    /// True when the resource is compiled in.
    /// </summary>
    /// <param name="name">Logical name.</param>
    public static bool Exists(string name) =>
        typeof(EmbeddedResources).Assembly.GetManifestResourceNames().Contains(name, StringComparer.Ordinal);
}
