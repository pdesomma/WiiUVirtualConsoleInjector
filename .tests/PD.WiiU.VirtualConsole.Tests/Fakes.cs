using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

internal sealed class FakeBaseStore : IBaseStore
{
    public List<string> Destinations { get; } = new();
    public string Root { get; init; } = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", "store");

    public TitleDirectory Locate(BaseTitle @base) => new(Path.Combine(Root, @base.TitleId.ToString()));

    public Task<TitleDirectory> StageAsync(BaseTitle @base, string destination, CancellationToken cancellationToken = default)
    {
        Destinations.Add(destination);
        return Task.FromResult(TestTitle.Populate(destination));
    }
}

internal sealed class FakeBootSoundConverter : IBootSoundConverter
{
    public List<(string Source, string Destination)> Calls { get; } = new();

    public Task ConvertAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        Calls.Add((sourcePath, destinationPath));
        File.WriteAllBytes(destinationPath, new byte[8]);
        return Task.CompletedTask;
    }
}

internal sealed class FakeImageConverter : IImageConverter
{
    public List<(string Source, ImageSlot Slot, string Destination)> Calls { get; } = new();

    public Task ConvertAsync(string sourcePath, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default)
    {
        Calls.Add((sourcePath, slot, destinationPath));
        File.WriteAllBytes(destinationPath, new byte[18]);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRomInjector : IRomInjector
{
    private readonly Func<Injection, TitleDirectory, Task>? _action;

    public FakeRomInjector(SourceConsole console, Func<Injection, TitleDirectory, Task>? action = null)
    {
        Console = console;
        _action = action;
    }

    public SourceConsole Console { get; }
    public List<BaseIssue> Issues { get; } = new();
    public List<TitleDirectory> Inspected { get; } = new();
    public List<TitleDirectory> Titles { get; } = new();

    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        Inspected.Add(title);
        return Issues;
    }

    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Titles.Add(title);
        progress?.Report("injecting");
        return _action?.Invoke(injection, title) ?? Task.CompletedTask;
    }
}

internal sealed class FakeTitlePacker : ITitlePacker
{
    public List<(TitleDirectory Title, string Output, string[] MetaFiles)> Calls { get; } = new();

    public Task PackAsync(TitleDirectory title, string outputDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((title, outputDirectory, Directory.GetFiles(title.Meta).Select(f => Path.GetFileName(f)).ToArray()));
        File.WriteAllText(Path.Combine(outputDirectory, "title.tmd"), "packed");
        return Task.CompletedTask;
    }
}

internal sealed class FakeKeyStore : IKeyStore
{
    private readonly Dictionary<TitleId, WiiUSharp.Nus.EncryptedTitleKey> _titleKeys = new();

    public AncastKey? AncastKey { get; set; }
    public WiiUSharp.Nus.CommonKey? CommonKey { get; set; }

    public WiiUSharp.Nus.EncryptedTitleKey? GetTitleKey(TitleId titleId) => _titleKeys.TryGetValue(titleId, out var key) ? key : null;

    public void SetTitleKey(TitleId titleId, WiiUSharp.Nus.EncryptedTitleKey? titleKey)
    {
        if (titleKey is null)
            _titleKeys.Remove(titleId);
        else
            _titleKeys[titleId] = titleKey.Value;
    }
}

internal sealed class FakeBaseDownloader : IBaseDownloader
{
    public List<(BaseTitle Base, WiiUSharp.Nus.EncryptedTitleKey TitleKey, WiiUSharp.Nus.CommonKey CommonKey)> Calls { get; } = new();
    public string Root { get; init; } = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", "store");

    public Task<TitleDirectory> DownloadAsync(BaseTitle @base, WiiUSharp.Nus.EncryptedTitleKey titleKey, WiiUSharp.Nus.CommonKey commonKey, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((@base, titleKey, commonKey));
        progress?.Report(new BaseDownloadProgress(BaseDownloadPhase.Downloading, "title.tmd", 1, 1, 0, null));
        return Task.FromResult(new TitleDirectory(Path.Combine(Root, @base.TitleId.ToString())));
    }
}
