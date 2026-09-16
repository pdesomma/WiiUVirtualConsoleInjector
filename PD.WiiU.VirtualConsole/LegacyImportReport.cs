namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What an import carried over; things already present were left alone.
/// </summary>
/// <param name="CommonKeyAdded">The Wii U common key was taken.</param>
/// <param name="TitleKeysAdded">Title keys taken.</param>
/// <param name="BasesAdded">Bases copied into the store.</param>
/// <param name="Failures">Bases that could not be copied, with why.</param>
public sealed record LegacyImportReport(bool CommonKeyAdded, int TitleKeysAdded, int BasesAdded, IReadOnlyList<string> Failures)
{
    /// <summary>
    /// True when nothing at all was taken.
    /// </summary>
    public bool IsEmpty => !CommonKeyAdded && TitleKeysAdded == 0 && BasesAdded == 0;
}
