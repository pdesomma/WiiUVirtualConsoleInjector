namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// The embedded wiivc_chan_booter builds that boot an installed Wii channel named by title.txt on the carrier disc.
/// </summary>
public static class ChannelBooter
{
    /// <summary>
    /// Resource name of the default build.
    /// </summary>
    public const string DefaultResourceName = "wiivc_chan_booter.dol";
    /// <summary>
    /// Resource name of the build that forces 4:3.
    /// </summary>
    public const string FourByThreeResourceName = "wiivc_chan_booter_force_4_by_3.dol";
    /// <summary>
    /// File on the carrier holding the four-character channel code.
    /// </summary>
    public const string TitleFileName = "title.txt";

    /// <summary>
    /// The booter shipped with the injector.
    /// </summary>
    /// <param name="forceFourByThree">Pick the build that forces 4:3.</param>
    public static byte[] Embedded(bool forceFourByThree = false)
    {
        var name = forceFourByThree ? FourByThreeResourceName : DefaultResourceName;
        using var stream = typeof(ChannelBooter).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded {name} is missing.");
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
