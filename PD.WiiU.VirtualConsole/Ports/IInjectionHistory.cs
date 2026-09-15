namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Remembers finished injects so they can be built again.
/// </summary>
public interface IInjectionHistory
{
    /// <summary>
    /// Raised after a record is added or removed.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Every record, newest first.
    /// </summary>
    IReadOnlyList<InjectionRecord> All();

    /// <summary>
    /// Keeps a record, copying its artwork, sound and icon somewhere that outlives the inject's work folder.
    /// </summary>
    /// <param name="record">Record with paths as they were at inject time.</param>
    /// <param name="iconTga">The iconTex.tga the title shipped with, or null.</param>
    /// <returns>The record as stored, with its paths pointing into the history.</returns>
    InjectionRecord Add(InjectionRecord record, byte[]? iconTga);

    /// <summary>
    /// Forgets a record and deletes its copies.
    /// </summary>
    /// <param name="id">Record id.</param>
    void Remove(string id);
}
