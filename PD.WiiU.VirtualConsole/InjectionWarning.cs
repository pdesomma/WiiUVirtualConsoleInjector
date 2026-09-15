namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Cautions the application shows before an inject; each can be silenced in settings.
/// </summary>
public enum InjectionWarning
{
    /// <summary>
    /// DSi-enhanced ROMs do not run.
    /// </summary>
    NdsDsiEnhanced,
    /// <summary>
    /// ROMs needing a co-processor do not run.
    /// </summary>
    SnesCoProcessor,
    /// <summary>
    /// GCZ images take longer and use more space than ISO.
    /// </summary>
    GameCubeGcz,
}
