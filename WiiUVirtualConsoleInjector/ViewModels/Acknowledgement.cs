namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One project or person this app owes something to.
/// </summary>
/// <param name="Name">Project or work.</param>
/// <param name="Authors">Who made it.</param>
/// <param name="Role">What was taken from it.</param>
/// <param name="Url">Where to find it, or null.</param>
/// <param name="License">Licence of anything shipped, or null when only ideas were borrowed.</param>
public sealed record Acknowledgement(string Name, string Authors, string Role, Uri? Url = null, string? License = null)
{
    /// <summary>
    /// True when a licence applies.
    /// </summary>
    public bool HasLicense => License is not null;
    /// <summary>
    /// True when there is somewhere to go.
    /// </summary>
    public bool HasUrl => Url is not null;
}
