namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Stages of an injection, in order.
/// </summary>
public enum InjectionStep
{
    StageBase,
    InspectBase,
    InjectRom,
    WriteMetadata,
    ConvertArtwork,
    ConvertBootSound,
    Pack,
}
