namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Stages of an injection, in order.
/// </summary>
public enum InjectionStep
{
    StageBase,
    InjectRom,
    WriteMetadata,
    ConvertArtwork,
    ConvertBootSound,
    Pack,
}
