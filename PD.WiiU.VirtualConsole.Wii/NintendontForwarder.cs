namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// The embedded Nintendont autoboot forwarders that become main.dol of a GameCube carrier disc.
/// </summary>
public static class NintendontForwarder
{
    /// <summary>
    /// Resource name of the default build.
    /// </summary>
    public const string DefaultResourceName = "nintendont_default_autobooter.dol";
    /// <summary>
    /// Resource name of the build that forces 4:3.
    /// </summary>
    public const string FourByThreeResourceName = "nintendont_force_4_by_3_autobooter.dol";

    /// <summary>
    /// The forwarder shipped with the injector.
    /// </summary>
    /// <param name="forceFourByThree">Pick the build that forces 4:3.</param>
    public static byte[] Embedded(bool forceFourByThree = false)
    {
        var name = forceFourByThree ? FourByThreeResourceName : DefaultResourceName;
        using var stream = typeof(NintendontForwarder).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded {name} is missing.");
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
