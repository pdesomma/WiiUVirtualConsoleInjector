namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What the community artwork repository holds for a game: the folder that matched and the files in it.
/// </summary>
/// <param name="Id">Repository path that matched, e.g. "gcn/GLME01".</param>
/// <param name="Icon">iconTex image.</param>
/// <param name="BootTv">bootTvTex image.</param>
/// <param name="BootDrc">bootDrcTex image, when the folder has one.</param>
/// <param name="GameIni">Emulator INI for an N64 game, when the folder has one.</param>
/// <param name="BootSound">Ready-made boot sound, when the folder has one.</param>
public sealed record CommunityArtworkHit(string Id, Uri Icon, Uri BootTv, Uri? BootDrc, Uri? GameIni, Uri? BootSound);
