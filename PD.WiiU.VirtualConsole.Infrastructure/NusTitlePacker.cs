using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Packing via WiiUSharp.Nus.
/// </summary>
public sealed class NusTitlePacker : ITitlePacker
{
    /// <summary>
    /// Title key used when none is given; the one NUSPacker defaults to.
    /// </summary>
    public const string DefaultTitleKey = "13371337133713371337133713371337";

    private readonly NusPacker _packer;

    /// <summary>
    /// Creates a new instance of the <see cref="NusTitlePacker"/> class.
    /// </summary>
    /// <param name="commonKey">Wraps the title key in the ticket.</param>
    /// <param name="titleKey">Encrypts the contents; <see cref="DefaultTitleKey"/> when null.</param>
    public NusTitlePacker(CommonKey commonKey, TitleKey? titleKey = null)
    {
        _packer = new NusPacker(commonKey);
        TitleKey = titleKey ?? WiiUSharp.Nus.TitleKey.Parse(DefaultTitleKey);
    }

    /// <summary>
    /// Encrypts the contents.
    /// </summary>
    public TitleKey TitleKey { get; }

    /// <inheritdoc/>
    public Task PackAsync(TitleDirectory title, string outputDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        _packer.Pack(title.Root, outputDirectory, TitleKey, progress: progress, cancellationToken: cancellationToken);
        return Task.CompletedTask;
    }
}
