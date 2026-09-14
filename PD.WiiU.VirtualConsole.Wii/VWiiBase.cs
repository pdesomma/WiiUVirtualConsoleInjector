using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// What every vWii base needs: the NFS key, a patchable IOS image and the disc container.
/// </summary>
public static class VWiiBase
{
    /// <summary>
    /// Lists what the base lacks.
    /// </summary>
    /// <param name="title">Base to inspect.</param>
    public static IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var inspection = new BaseInspection(title);
        var key = TitleDirectory.CodeFolder + "/" + WiiRomInjector.NfsKeyFileName;
        if (inspection.RequireFile(key, NfsFormat.KeySize))
            inspection.Require(new FileInfo(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName)).Length == NfsFormat.KeySize, key, $"expected exactly {NfsFormat.KeySize} bytes");

        var firmware = TitleDirectory.CodeFolder + "/" + WiiRomInjector.FirmwareFileName;
        if (inspection.RequireFile(firmware, 1))
        {
            var revision = FirmwarePatcher.ReadRevision(File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName)));
            inspection.Require(revision == FirmwarePatcher.SupportedRevision, firmware, $"IOS revision {revision ?? "unknown"}, patches target {FirmwarePatcher.SupportedRevision}");
        }

        inspection.RequireAny(TitleDirectory.ContentFolder, "hif_*.nfs");
        return inspection.Issues;
    }
}
