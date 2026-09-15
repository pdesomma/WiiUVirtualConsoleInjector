using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Tests;

internal sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public List<(string Title, string Message)> Errors { get; } = new();
    public string? FileToPick { get; set; }
    public string? FolderToPick { get; set; }
    public List<(string Title, string Message)> Infos { get; } = new();
    public List<string> PickedFolderStarts { get; } = new();

    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(ConfirmResult);

    public Task<string?> PickFolderAsync(string title, string? startFolder = null)
    {
        PickedFolderStarts.Add(startFolder ?? string.Empty);
        return Task.FromResult(FolderToPick);
    }

    public Task<string?> PickOpenFileAsync(string title, params FileFilter[] filters) => Task.FromResult(FileToPick);

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add((title, message));
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        Infos.Add((title, message));
        return Task.CompletedTask;
    }
}

internal sealed class FakeSettingsService : ISettingsService
{
    public const string DefaultBase = @"C:\data\bases";
    public const string DefaultOutput = @"C:\data\output";
    public const string DefaultWork = @"C:\data\work";

    public event EventHandler? Changed;

    public string BasePath => Current.BasePath ?? DefaultBase;
    public AppSettings Current { get; set; } = new();
    public string OutputPath => Current.OutputPath ?? DefaultOutput;
    public int Saves { get; private set; }
    public string WorkPath => Current.WorkPath ?? DefaultWork;

    public void Update(Func<AppSettings, AppSettings> change)
    {
        Current = change(Current);
        Saves++;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeKeyStore : IKeyStore
{
    private readonly Dictionary<TitleId, EncryptedTitleKey> _titleKeys = new();

    public AncastKey? AncastKey { get; set; }
    public CommonKey? CommonKey { get; set; }
    public WiiSharp.CommonKey? WiiCommonKey { get; set; }

    public EncryptedTitleKey? GetTitleKey(TitleId titleId) => _titleKeys.TryGetValue(titleId, out var key) ? key : null;

    public void SetTitleKey(TitleId titleId, EncryptedTitleKey? titleKey)
    {
        if (titleKey is null)
            _titleKeys.Remove(titleId);
        else
            _titleKeys[titleId] = titleKey.Value;
    }
}

internal sealed class FakeBaseService : IBaseService
{
    public Func<BaseTitle, IProgress<BaseDownloadProgress>?, CancellationToken, Task<TitleDirectory>> Download { get; set; } =
        (b, _, _) => Task.FromResult(new TitleDirectory(Path.Combine(Path.GetTempPath(), b.TitleId.ToString())));
    public List<BaseTitle> Downloads { get; } = new();
    public HashSet<TitleId> Present { get; } = new();
    public List<BaseTitle> Titles { get; } = new();

    public IReadOnlyList<BaseTitle> Available(SourceConsole console) => Titles.Where(t => t.Console == console).ToArray();

    public Task<TitleDirectory> DownloadAsync(BaseTitle @base, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Downloads.Add(@base);
        return Download(@base, progress, cancellationToken);
    }

    public BaseStatus Status(BaseTitle @base)
    {
        if (Present.Contains(@base.TitleId))
            return BaseStatus.Present;
        if (Keys.CommonKey is null)
            return BaseStatus.NeedsCommonKey;
        if (Keys.GetTitleKey(@base.TitleId) is null)
            return BaseStatus.NeedsTitleKey;
        return BaseStatus.Downloadable;
    }

    public FakeKeyStore Keys { get; init; } = new();
}

internal sealed class FakeInjectionService : IInjectionService
{
    public List<BaseTitle> Inspected { get; } = new();
    public List<BaseIssue> Issues { get; } = new();

    public IReadOnlyList<BaseIssue> InspectBase(BaseTitle @base)
    {
        Inspected.Add(@base);
        return Issues;
    }

    public Task<InjectedTitle> InjectAsync(Injection injection, string workDirectory, string outputDirectory, IProgress<InjectionProgress>? progress = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class FakeInjectionServiceFactory : IInjectionServiceFactory
{
    public Exception? CreateError { get; set; }
    public int Creates { get; private set; }
    public FakeInjectionService Service { get; } = new();

    public IInjectionService Create()
    {
        Creates++;
        if (CreateError is not null)
            throw CreateError;
        return Service;
    }

    public IReadOnlyList<string> MissingKeys(SourceConsole console) => Array.Empty<string>();
}
