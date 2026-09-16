namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Files downloaded from one community artwork folder.
/// </summary>
/// <param name="Id">Repository path they came from.</param>
/// <param name="Icon">Local iconTex image.</param>
/// <param name="BootTv">Local bootTvTex image.</param>
/// <param name="BootDrc">Local bootDrcTex image, if the folder had one.</param>
/// <param name="GameIni">Local emulator INI, if the folder had one.</param>
/// <param name="BootSound">Local boot sound, if the folder had one.</param>
public sealed record CommunityArtworkFiles(string Id, string Icon, string BootTv, string? BootDrc, string? GameIni, string? BootSound);
