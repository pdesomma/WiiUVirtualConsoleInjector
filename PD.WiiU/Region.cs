namespace PD.WiiU;

/// <summary>
/// Regions a title is allowed to run in. A bitmask; combine values for multi-region titles.
/// </summary>
[Flags]
public enum Region : uint
{
    None = 0,
    Japan = 1 << 0,
    UnitedStates = 1 << 1,
    Europe = 1 << 2,
    Australia = 1 << 3,
    China = 1 << 4,
    Korea = 1 << 5,
    Taiwan = 1 << 6,

    /// <summary>Region-free: runs on any console.</summary>
    All = 0xFFFFFFFF,
}
