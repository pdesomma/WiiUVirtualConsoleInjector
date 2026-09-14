using PD.WiiU.VirtualConsole.Options;
using WiiSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Rewrites a disc image's region area to match <see cref="WiiOptions.TargetRegion"/>.
/// </summary>
public static class RegionPatcher
{
    /// <summary>
    /// Applies the target region, if any, to the image in place.
    /// </summary>
    /// <param name="disc">Seekable, writable disc image or NFS payload.</param>
    /// <param name="options">Wii settings.</param>
    /// <returns>True if the image was changed.</returns>
    public static bool Apply(Stream disc, WiiOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (options.TargetRegion is not { } target)
            return false;

        RegionArea.Write(disc, RegionSettings.Preset(ToDiscRegion(target)));
        return true;
    }

    /// <summary>
    /// Disc region for a Wii U region: Japan and the United States map directly, everything else is Europe.
    /// </summary>
    /// <param name="region">Wii U region.</param>
    /// <exception cref="ArgumentOutOfRangeException">No region bits set.</exception>
    public static DiscRegion ToDiscRegion(Region region)
    {
        if (region == Region.None)
            throw new ArgumentOutOfRangeException(nameof(region), region, "A target region is required.");
        if (region == Region.Japan)
            return DiscRegion.Japan;
        if (region == Region.UnitedStates)
            return DiscRegion.UnitedStates;
        return DiscRegion.Europe;
    }
}
