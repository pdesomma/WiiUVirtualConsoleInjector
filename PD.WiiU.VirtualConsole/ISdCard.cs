namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The SD card: which drive it is, and copying a packed title onto it.
/// </summary>
public interface ISdCard
{
    /// <summary>
    /// The removable volumes on offer.
    /// </summary>
    IReadOnlyList<RemovableDrive> Drives();

    /// <summary>
    /// The drive that looks like the Wii U's card, or null when that cannot be told.
    /// </summary>
    RemovableDrive? Detect();

    /// <summary>
    /// Copies a finished title onto the card and returns where it landed: packed titles under install, Loadiine ones under wiiu/games.
    /// </summary>
    /// <param name="titleDirectory">Folder holding the packed title.</param>
    /// <param name="root">Card root to copy onto.</param>
    /// <param name="progress">File names as they are copied.</param>
    /// <param name="cancellationToken">Stops the copy.</param>
    /// <exception cref="DirectoryNotFoundException">The title folder is not there.</exception>
    /// <exception cref="IOException">The card has too little room.</exception>
    Task<string> CopyAsync(string titleDirectory, string root, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts a single file onto the card at its card path, making the folders, and returns where it landed.
    /// </summary>
    /// <param name="file">File and where it goes.</param>
    /// <param name="root">Card root to copy onto.</param>
    /// <param name="cancellationToken">Stops the copy.</param>
    /// <exception cref="FileNotFoundException">The source is not there.</exception>
    /// <exception cref="IOException">The card has too little room.</exception>
    Task<string> CopyAsync(CardFile file, string root, CancellationToken cancellationToken = default);
}
