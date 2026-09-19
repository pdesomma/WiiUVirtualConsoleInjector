using System.Collections.ObjectModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

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

    public List<(string Title, string Path)> Images { get; } = new();

    public Task ShowImageAsync(string title, string path)
    {
        Images.Add((title, path));
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
    public string? CaptionFontPath => Current.CaptionFontPath ?? FoundCaptionFont;
    public AppSettings Current { get; set; } = new();
    public string? FoundCaptionFont { get; set; }
    public string OutputPath => Current.OutputPath ?? DefaultOutput;
    public int Saves { get; private set; }
    public string DetectedSdPath { get; set; } = string.Empty;
    public string SdPath => Current.SdPath ?? DetectedSdPath;
    public string WorkPath => Current.WorkPath ?? DefaultWork;

    public void Update(Func<AppSettings, AppSettings> change)
    {
        Current = change(Current);
        Saves++;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeArtworkComposer : IArtworkComposer
{
    public List<(ArtworkRequest Request, ImageSlot Slot, string Path)> Composed { get; } = new();
    public Exception? Failure { get; set; }

    public Task ComposeAsync(ArtworkRequest request, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default)
    {
        Composed.Add((request, slot, destinationPath));
        if (Failure is not null)
            return Task.FromException(Failure);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllBytes(destinationPath, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        return Task.CompletedTask;
    }
}

internal sealed class FakeSoundPlayer : ISoundPlayer
{
    public Exception? Failure { get; set; }
    public bool IsPlaying { get; private set; }
    public List<string> Played { get; } = new();
    public int Stops { get; private set; }

    public event EventHandler? Stopped;

    public void Finish()
    {
        IsPlaying = false;
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    public void Play(string path)
    {
        if (Failure is not null)
            throw Failure;

        Played.Add(path);
        IsPlaying = true;
    }

    public void Stop()
    {
        Stops++;
        if (IsPlaying)
            Finish();
    }
}

internal sealed class FakeSdCard : ISdCard
{
    public List<(string Title, string Root)> Copies { get; } = new();
    public List<(CardFile File, string Root)> FileCopies { get; } = new();
    public string CopyResult { get; set; } = @"E:\install\[WUP]Test";
    public RemovableDrive? Detected { get; set; }
    public Exception? Failure { get; set; }
    public List<RemovableDrive> Removable { get; } = new();

    public Task<string> CopyAsync(string titleDirectory, string root, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Copies.Add((titleDirectory, root));
        if (Failure is not null)
            return Task.FromException<string>(Failure);

        progress?.Report("title.tmd");
        return Task.FromResult(CopyResult);
    }

    public Task<string> CopyAsync(CardFile file, string root, CancellationToken cancellationToken = default)
    {
        FileCopies.Add((file, root));
        return Failure is not null ? Task.FromException<string>(Failure) : Task.FromResult(file.On(root));
    }

    public RemovableDrive? Detect() => Detected;

    public IReadOnlyList<RemovableDrive> Drives() => Removable;
}

/// <summary>
/// Runs posted work at once, so a Progress&lt;T&gt; report lands before the awaiting command continues, as it does on the UI thread.
/// </summary>
internal sealed class InlineSynchronizationContext : SynchronizationContext
{
    public static void Install() => SetSynchronizationContext(new InlineSynchronizationContext());

    public override void Post(SendOrPostCallback d, object? state) => d(state);

    public override void Send(SendOrPostCallback d, object? state) => d(state);
}

internal sealed class FakeDiscBackup : IDiscBackup
{
    public Exception? Failure { get; set; }
    public List<string> Folders { get; } = new();
    public List<(string Image, WiiUSharp.Wud.DiscKey? DiscKey, string Output)> Unpacked { get; } = new();

    public bool Accepts(string imagePath) => imagePath.EndsWith(".wud", StringComparison.OrdinalIgnoreCase) || imagePath.EndsWith(".wux", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<string>> UnpackAsync(string imagePath, WiiUSharp.Wud.DiscKey? discKey, CommonKey commonKey, string outputFolder, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Unpacked.Add((imagePath, discKey, outputFolder));
        if (Failure is not null)
            return Task.FromException<IReadOnlyList<string>>(Failure);
        progress?.Report("00000000.app");
        return Task.FromResult<IReadOnlyList<string>>(Folders);
    }
}

internal sealed class FakeLegacyImport : ILegacyImport
{
    public Exception? Failure { get; set; }
    public LegacyInstall? Found { get; set; }
    public List<LegacyInstall> Imported { get; } = new();
    public LegacyImportReport Report { get; set; } = new(false, 0, 0, Array.Empty<string>());

    public LegacyInstall? Find() => Found;

    public Task<LegacyImportReport> ImportAsync(LegacyInstall install, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Imported.Add(install);
        if (Failure is not null)
            return Task.FromException<LegacyImportReport>(Failure);
        progress?.Report("Copying");
        return Task.FromResult(Report);
    }
}

internal sealed class FakeUpdateCheck : IUpdateCheck
{
    public int Calls { get; private set; }
    public Exception? Failure { get; set; }
    public AppRelease? Latest { get; set; }

    public Task<AppRelease?> LatestAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Failure is null ? Task.FromResult(Latest) : Task.FromException<AppRelease?>(Failure);
    }
}

internal sealed class FakeNintendontSource : INintendontSource
{
    public Exception? Failure { get; set; }
    public List<string> Installs { get; } = new();

    public Task InstallAsync(string cardRoot, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Installs.Add(cardRoot);
        if (Failure is not null)
            return Task.FromException(Failure);
        var folder = Path.Combine(cardRoot, "apps", "nintendont");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "boot.dol"), new byte[] { 1 });
        progress?.Report("boot.dol");
        return Task.CompletedTask;
    }
}

internal sealed class FakeCommunityArtwork : ICommunityArtwork
{
    public List<Uri> Downloaded { get; } = new();
    public Exception? Failure { get; set; }
    public CommunityArtworkHit? Hit { get; set; }
    public List<IReadOnlyList<string>> Lookups { get; } = new();

    public Task DownloadAsync(Uri source, string destinationPath, CancellationToken cancellationToken = default)
    {
        Downloaded.Add(source);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllBytes(destinationPath, new byte[] { 1, 2, 3 });
        return Task.CompletedTask;
    }

    public Task<CommunityArtworkHit?> FindAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        Lookups.Add(ids);
        if (Failure is not null)
            throw Failure;
        return Task.FromResult(Hit);
    }
}

internal sealed class FakeCompatibilityLists : ICompatibilityLists
{
    public List<SourceConsole> Opened { get; } = new();

    public Uri For(SourceConsole console) => new($"https://example.test/{console}");

    public Task<bool> OpenAsync(SourceConsole console)
    {
        Opened.Add(console);
        return Task.FromResult(true);
    }
}

internal sealed class FakeLinkOpener : ILinkOpener
{
    public List<string> Folders { get; } = new();
    public List<Uri> Opened { get; } = new();

    public Task<bool> OpenAsync(Uri uri)
    {
        Opened.Add(uri);
        return Task.FromResult(true);
    }

    public Task<bool> OpenFolderAsync(string path)
    {
        Folders.Add(path);
        return Task.FromResult(true);
    }
}

internal sealed class FakeToastService : IToastService
{
    private readonly ObservableCollection<ToastViewModel> _toasts = new();

    public FakeToastService()
    {
        Toasts = new ReadOnlyObservableCollection<ToastViewModel>(_toasts);
    }

    public List<ToastViewModel> Dismissed { get; } = new();
    public List<(ToastKind Kind, string Title, string? Message)> Shown { get; } = new();
    public ReadOnlyObservableCollection<ToastViewModel> Toasts { get; }

    public void Dismiss(ToastViewModel toast) => Dismissed.Add(toast);

    public void Show(ToastKind kind, string title, string? message = null)
    {
        Shown.Add((kind, title, message));
        _toasts.Add(new ToastViewModel(kind, title, message, this));
    }
}

internal sealed class FakeUiScheduler : IUiScheduler
{
    private readonly List<(TimeSpan Delay, Action Action)> _pending = new();

    public int Cancelled { get; private set; }
    public int Posted { get; private set; }
    public IReadOnlyList<(TimeSpan Delay, Action Action)> Pending => _pending;

    public IDisposable Delay(TimeSpan delay, Action action)
    {
        var entry = (delay, action);
        _pending.Add(entry);
        return new Cancellation(() =>
        {
            if (_pending.Remove(entry))
                Cancelled++;
        });
    }

    public void Post(Action action)
    {
        Posted++;
        action();
    }

    public void RunDue(TimeSpan delay)
    {
        foreach (var entry in _pending.Where(p => p.Delay == delay).ToArray())
        {
            _pending.Remove(entry);
            entry.Action();
        }
    }

    private sealed class Cancellation : IDisposable
    {
        private readonly Action _cancel;

        public Cancellation(Action cancel) => _cancel = cancel;

        public void Dispose() => _cancel();
    }
}

internal sealed class FakeKeyStore : IKeyStore
{
    private readonly Dictionary<TitleId, EncryptedTitleKey> _titleKeys = new();

    public CommonKey? CommonKey { get; set; }
    public int TitleKeyWrites { get; private set; }
    public WiiSharp.CommonKey? WiiCommonKey { get; set; }

    public EncryptedTitleKey? GetTitleKey(TitleId titleId) => _titleKeys.TryGetValue(titleId, out var key) ? key : null;

    public void SetTitleKey(TitleId titleId, EncryptedTitleKey? titleKey)
    {
        TitleKeyWrites++;
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

    public Exception? ImportFailure { get; set; }
    public List<(BaseTitle Base, string Folder)> Imports { get; } = new();

    public void AddCustom(BaseTitle @base)
    {
        Titles.RemoveAll(t => t.TitleId == @base.TitleId);
        Titles.Add(new BaseTitle(@base.TitleId, @base.Name, @base.Region, @base.Console) { IsCustom = true });
    }

    public IReadOnlyList<BaseTitle> Available(SourceConsole console) => Titles.Where(t => t.Console == console).ToArray();

    public Task<TitleDirectory> DownloadAsync(BaseTitle @base, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Downloads.Add(@base);
        return Download(@base, progress, cancellationToken);
    }

    public Task<TitleDirectory> ImportAsync(BaseTitle @base, string sourceDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (ImportFailure is not null)
            throw ImportFailure;
        Imports.Add((@base, sourceDirectory));
        progress?.Report("Copying " + Path.GetFileName(sourceDirectory));
        Present.Add(@base.TitleId);
        return Task.FromResult(new TitleDirectory(Path.Combine(Path.GetTempPath(), @base.TitleId.ToString())));
    }

    public bool RemoveCustom(TitleId titleId) => Titles.RemoveAll(t => t.TitleId == titleId && t.IsCustom) > 0;

    public bool HasTitleKey(BaseTitle @base) => Keys.GetTitleKey(@base.TitleId) is not null;

    public Dictionary<TitleId, ByteSize> Sizes { get; } = new();

    public ByteSize? SizeOnDisk(BaseTitle @base) => Present.Contains(@base.TitleId) ? Sizes.TryGetValue(@base.TitleId, out var size) ? size : new ByteSize(0) : null;

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
    public string? Fit { get; set; }

    public string? RomFit(BaseTitle @base, string romPath) => Fit;

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

    public IReadOnlyList<string> MissingKeys(SourceConsole console, string? romPath = null) => Array.Empty<string>();
}

internal sealed class FakeInjectionHistory : IInjectionHistory
{
    public List<(InjectionRecord Record, byte[]? IconTga)> Added { get; } = new();
    public Exception? AddError { get; set; }
    public List<InjectionRecord> Records { get; } = new();
    public List<string> Removed { get; } = new();

    public event EventHandler? Changed;

    public InjectionRecord Add(InjectionRecord record, byte[]? iconTga)
    {
        if (AddError is not null)
            throw AddError;
        Added.Add((record, iconTga));
        Records.Insert(0, record);
        Changed?.Invoke(this, EventArgs.Empty);
        return record;
    }

    public IReadOnlyList<InjectionRecord> All() => Records.ToArray();

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Remove(string id)
    {
        Removed.Add(id);
        Records.RemoveAll(r => r.Id == id);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
