using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Bases the user added beyond the bundled catalog, kept across runs.
/// </summary>
public interface ICustomBases
{
    /// <summary>
    /// Remembers a base; replaces one with the same title ID.
    /// </summary>
    /// <param name="base">The base; stored as custom whatever its flag says.</param>
    void Add(BaseTitle @base);

    /// <summary>
    /// Every remembered base, flagged custom.
    /// </summary>
    IReadOnlyList<BaseTitle> All();

    /// <summary>
    /// Forgets a base.
    /// </summary>
    /// <param name="titleId">Its title ID.</param>
    /// <returns>False when nothing was remembered under it.</returns>
    bool Remove(TitleId titleId);
}
