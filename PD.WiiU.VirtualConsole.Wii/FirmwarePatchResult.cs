namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// How many places a patch was applied.
/// </summary>
/// <param name="Patch">Patch applied.</param>
/// <param name="Matches">Pattern matches found and patched; zero usually means an unexpected fw.img revision.</param>
public readonly record struct FirmwarePatchResult(FirmwarePatch Patch, int Matches);
