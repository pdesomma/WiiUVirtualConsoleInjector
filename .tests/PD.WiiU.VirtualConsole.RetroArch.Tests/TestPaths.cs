namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

/// <summary>
/// Throwaway folders under the temp directory, one per test.
/// </summary>
internal static class TestPaths
{
    public static string TempRoot() =>
        Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.RetroArch.Tests", Guid.NewGuid().ToString("N"));
}
