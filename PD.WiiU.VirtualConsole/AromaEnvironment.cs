namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What an SD card says about Aroma: whether the environment is on it and whether it carries signature patches, which RetroArch titles need to install.
/// </summary>
public sealed class AromaEnvironment
{
    /// <summary>
    /// Folder on the card that holds the environment.
    /// </summary>
    public const string EnvironmentFolder = "wiiu/environments/aroma";
    /// <summary>
    /// The setup module that patches signature checks; without it every fake-signed title fails to install.
    /// </summary>
    public const string SigPatchesFile = EnvironmentFolder + "/modules/setup/01_sigpatches.rpx";

    private AromaEnvironment(bool isInstalled, bool hasSigPatches)
    {
        IsInstalled = isInstalled;
        HasSigPatches = hasSigPatches;
    }

    /// <summary>
    /// True when the signature patch module is in the environment's setup folder.
    /// </summary>
    public bool HasSigPatches { get; }
    /// <summary>
    /// True when the environment folder is on the card.
    /// </summary>
    public bool IsInstalled { get; }

    /// <summary>
    /// Looks at a card root.
    /// </summary>
    /// <param name="sdRoot">Root of the card.</param>
    public static AromaEnvironment Inspect(string sdRoot)
    {
        if (string.IsNullOrWhiteSpace(sdRoot))
            throw new ArgumentException("Card root is required.", nameof(sdRoot));

        var installed = Directory.Exists(At(sdRoot, EnvironmentFolder));
        return new AromaEnvironment(installed, installed && File.Exists(At(sdRoot, SigPatchesFile)));
    }

    private static string At(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
}
