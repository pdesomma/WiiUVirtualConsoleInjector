using System.Text;
using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Applies byte patches to the vWii IOS image. Patterns target IOS revision r590, the one every vWii base ships.
/// </summary>
public static class FirmwarePatcher
{
    /// <summary>
    /// Revision the patterns were written for.
    /// </summary>
    public const string SupportedRevision = "r590";

    private static readonly byte[] RevisionMarker = Encoding.ASCII.GetBytes("svn-");

    private static readonly IReadOnlyDictionary<FirmwarePatch, BytePatch[]> Table = new Dictionary<FirmwarePatch, BytePatch[]>
    {
        [FirmwarePatch.FakeSign] = new[]
        {
            BytePatch.At(new byte[] { 0x20, 0x07, 0x23, 0xA2 }, 1, 0x00),
            BytePatch.At(new byte[] { 0x20, 0x07, 0x4B, 0x0B }, 1, 0x00),
        },
        [FirmwarePatch.ShoulderToTrigger] = new[]
        {
            BytePatch.Replace(new byte[] { 0x40, 0x05, 0x46, 0xA9 }, 0x26, 0x80, 0x40, 0x06),
            BytePatch.Replace(new byte[] { 0x1C, 0x05, 0x40, 0x35 }, 0x25, 0x40, 0x40, 0x05),
            BytePatch.Replace(new byte[] { 0x23, 0x7F, 0x1C, 0x02 }, 0x46, 0xB1, 0x23, 0x20, 0x40, 0x03),
            BytePatch.Replace(new byte[] { 0x46, 0x53, 0x42, 0x18 }, 0x23, 0x10, 0x40, 0x03),
            BytePatch.Replace(new byte[] { 0x1C, 0x05, 0x80, 0x22 }, 0x25, 0x40, 0x80, 0x22, 0x40, 0x05),
        },
        [FirmwarePatch.WiiRemote] = new[]
        {
            BytePatch.Replace(new byte[] { 0x16, 0x13, 0x1C, 0x02, 0x40, 0x9A, 0x1C, 0x13 }, 0x23, 0x00),
        },
        [FirmwarePatch.HorizontalWiiRemote] = new[]
        {
            new BytePatch(
                Exact(0x4A, 0x71, 0x42, 0x13, 0xD0, 0xD2, 0x9B, 0x00),
                (0x07, new byte[] { 0x02 }),
                (0x0F, new byte[] { 0x03 }),
                (0x1D, new byte[] { 0x01 }),
                (0x2B, new byte[] { 0x00 }),
                (0x65, new byte[] { 0x07 }),
                (0x75, new byte[] { 0x06 }),
                (0x85, new byte[] { 0x04 }),
                (0x95, new byte[] { 0x05 })),
        },
        [FirmwarePatch.Homebrew] = new[]
        {
            BytePatch.Replace(new byte[] { 0xD0, 0x0B, 0x23, 0x08, 0x43, 0x13, 0x60, 0x0B }, 0x46, 0xC0),
            BytePatch.At(new byte[] { 0x01, 0x94, 0xB5, 0x00, 0x4B, 0x08, 0x22, 0x01 }, 6, 0x22, 0x00),
            BytePatch.At(new byte[] { 0xB0, 0xBA, 0x1C, 0x0F }, -12,
                0xE5, 0x9F, 0x10, 0x04, 0xE5, 0x91, 0x00, 0x00, 0xE1, 0x2F, 0xFF, 0x10, 0x12, 0xFF, 0xFF, 0xE0),
            BytePatch.Replace(new byte[] { 0x68, 0x4B, 0x2B, 0x06 },
                0x49, 0x01, 0x47, 0x88, 0x46, 0xC0, 0xE0, 0x01, 0x12, 0xFF, 0xFE, 0x00, 0x22, 0x00, 0x23, 0x01, 0x46, 0xC0, 0x46, 0xC0),
            new BytePatch(
                Exact(0x0D, 0x80, 0x00, 0x00, 0x0D, 0x80, 0x00, 0x00).Concat(Any(8)).Concat(Exact(0x00, 0x00, 0x00, 0x02)).ToArray(),
                (0x10, new byte[] { 0x00, 0x00, 0x00, 0x03 })),
        },
        [FirmwarePatch.Passthrough] = new[]
        {
            BytePatch.At(new byte[] { 0x20, 0x4B, 0x01, 0x68, 0x18, 0x47, 0x70, 0x00 }, 3, 0x20, 0x00),
            BytePatch.Replace(new byte[] { 0x28, 0x00, 0xD0, 0x03, 0x49, 0x02, 0x22, 0x09 },
                0xF0, 0x04, 0xFF, 0x21, 0x48, 0x02, 0x21, 0x09, 0xF0, 0x04, 0xFE, 0xF9),
            BytePatch.Replace(new byte[] { 0xF0, 0x01, 0xFA, 0xB9 }, 0xF7, 0xFC, 0xFB, 0x95),
        },
        [FirmwarePatch.InstantClassicController] = new[]
        {
            BytePatch.At(new byte[] { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xD1, 0xB7 }, 6, 0x46, 0xC0),
        },
        [FirmwarePatch.NoClassicController] = new[]
        {
            BytePatch.At(new byte[] { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xD1, 0xB7 }, 6, 0xE0, 0xB7),
        },
    };

    /// <summary>
    /// Applies patches in place.
    /// </summary>
    /// <param name="image">fw.img contents.</param>
    /// <param name="patches">Patches to apply; duplicates are applied once.</param>
    /// <returns>One result per distinct patch, in the order given.</returns>
    public static IReadOnlyList<FirmwarePatchResult> Apply(byte[] image, IEnumerable<FirmwarePatch> patches)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        if (patches is null)
            throw new ArgumentNullException(nameof(patches));

        var results = new List<FirmwarePatchResult>();
        foreach (var patch in patches.Distinct())
        {
            if (!Table.TryGetValue(patch, out var steps))
                throw new ArgumentOutOfRangeException(nameof(patches), patch, "Unknown patch.");
            results.Add(new FirmwarePatchResult(patch, steps.Sum(s => s.Apply(image))));
        }
        return results;
    }

    /// <summary>
    /// Patches the settings call for. Always includes <see cref="FirmwarePatch.FakeSign"/>.
    /// </summary>
    /// <param name="options">Wii settings.</param>
    /// <param name="homebrew">The injected title is homebrew rather than a retail disc.</param>
    public static IReadOnlyList<FirmwarePatch> PatchesFor(WiiOptions options, bool homebrew = false)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var patches = new List<FirmwarePatch> { FirmwarePatch.FakeSign };
        switch (options.ControllerMode)
        {
            case WiiControllerMode.WiiRemote:
                patches.Add(FirmwarePatch.WiiRemote);
                break;
            case WiiControllerMode.HorizontalWiiRemote:
                patches.Add(FirmwarePatch.WiiRemote);
                patches.Add(FirmwarePatch.HorizontalWiiRemote);
                break;
            case WiiControllerMode.InstantClassicController:
                patches.Add(FirmwarePatch.InstantClassicController);
                break;
            case WiiControllerMode.NoClassicController:
                patches.Add(FirmwarePatch.NoClassicController);
                break;
        }
        if (options.LrPatch)
            patches.Add(FirmwarePatch.ShoulderToTrigger);
        if (homebrew)
            patches.Add(FirmwarePatch.Homebrew);
        if (options.Passthrough)
            patches.Add(FirmwarePatch.Passthrough);
        return patches;
    }

    /// <summary>
    /// Reads and patches a file in place.
    /// </summary>
    /// <param name="path">fw.img path.</param>
    /// <param name="patches">Patches to apply.</param>
    public static IReadOnlyList<FirmwarePatchResult> PatchFile(string path, IEnumerable<FirmwarePatch> patches)
    {
        var image = File.ReadAllBytes(path);
        var results = Apply(image, patches);
        File.WriteAllBytes(path, image);
        return results;
    }

    /// <summary>
    /// IOS revision from the embedded "svn-rNNN" marker, or null if absent.
    /// </summary>
    /// <param name="image">fw.img contents.</param>
    public static string? ReadRevision(byte[] image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        for (var offset = 0; offset + RevisionMarker.Length + 4 <= image.Length; offset++)
        {
            var i = 0;
            while (i < RevisionMarker.Length && image[offset + i] == RevisionMarker[i])
                i++;
            if (i == RevisionMarker.Length)
                return Encoding.ASCII.GetString(image, offset + RevisionMarker.Length, 4);
        }
        return null;
    }

    private static byte?[] Any(int count) => new byte?[count];

    private static byte?[] Exact(params byte[] bytes) => bytes.Select(b => (byte?)b).ToArray();
}
