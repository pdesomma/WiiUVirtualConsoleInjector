using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What <see cref="BaseFolder.Inspect"/> found: the folder's kind and whatever identity its files gave away.
/// </summary>
/// <param name="Kind">Package or plain title.</param>
/// <param name="TitleId">Title ID, when readable.</param>
/// <param name="Name">Long name, when a meta.xml gave one.</param>
/// <param name="Region">Region, when a meta.xml named one of ours.</param>
public sealed record BaseFolderInfo(BaseFolderKind Kind, TitleId? TitleId, string? Name, Region? Region);
