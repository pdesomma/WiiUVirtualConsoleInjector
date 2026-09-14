namespace PD.WiiU;

/// <summary>
/// The high 32 bits of a <see cref="TitleId"/>. Tells the system what kind of title it is
/// and therefore where it installs and how the menu treats it.
/// </summary>
public enum TitleType : uint
{
    /// <summary>Retail disc and eShop games.</summary>
    Game = 0x00050000,

    /// <summary>Demos and kiosk builds. Virtual Console injections use this type.</summary>
    Demo = 0x00050002,

    /// <summary>Downloadable content for a <see cref="Game"/> title.</summary>
    Dlc = 0x0005000C,

    /// <summary>Title updates (patches) for a <see cref="Game"/> title.</summary>
    Update = 0x0005000E,

    /// <summary>System applications such as the eShop and Mii Maker.</summary>
    SystemApplication = 0x00050010,

    /// <summary>System data titles such as fonts and certificates.</summary>
    SystemData = 0x0005001B,

    /// <summary>System applets such as the Internet Browser and Home Menu overlays.</summary>
    SystemApplet = 0x00050030,
}
