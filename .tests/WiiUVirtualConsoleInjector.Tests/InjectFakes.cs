using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Tests;

/// <summary>
/// Test doubles for the inject page.
/// </summary>
internal static class InjectFakes
{
    public static BaseTitle Base(SourceConsole console, uint id = 0x10101D00, string name = "Base") =>
        new(new TitleId(TitleType.Game, id), name, Region.UnitedStates, console);

    /// <summary>
    /// Runs posted callbacks inline so Progress callbacks land before the awaited call returns.
    /// </summary>
    internal sealed class InlineSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);

        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }

    internal sealed class InjectDialogService : IDialogService
    {
        public bool ConfirmResult { get; set; } = true;
        public List<(string Title, string Message)> Confirms { get; } = new();
        public List<(string Title, string Message)> Errors { get; } = new();
        public List<(string Title, string Message)> Infos { get; } = new();
        public List<string> FolderPicks { get; } = new();
        public Queue<string?> NextPaths { get; } = new();
        public List<(string Title, FileFilter[] Filters)> FilePicks { get; } = new();

        public Task<bool> ConfirmAsync(string title, string message)
        {
            Confirms.Add((title, message));
            return Task.FromResult(ConfirmResult);
        }

        public Task<string?> PickFolderAsync(string title, string? startFolder = null)
        {
            FolderPicks.Add(title);
            return Task.FromResult(NextPaths.Count == 0 ? null : NextPaths.Dequeue());
        }

        public Task<string?> PickOpenFileAsync(string title, params FileFilter[] filters)
        {
            FilePicks.Add((title, filters));
            return Task.FromResult(NextPaths.Count == 0 ? null : NextPaths.Dequeue());
        }

        public Task ShowErrorAsync(string title, string message)
        {
            Errors.Add((title, message));
            return Task.CompletedTask;
        }

        public Action? OnInfo { get; set; }

        public List<(string Title, string Path)> Images { get; } = new();

        public Task ShowImageAsync(string title, string path)
        {
            Images.Add((title, path));
            return Task.CompletedTask;
        }

        public Task ShowInfoAsync(string title, string message)
        {
            Infos.Add((title, message));
            OnInfo?.Invoke();
            return Task.CompletedTask;
        }
    }

    internal sealed class InjectSettingsService : ISettingsService
    {
        public event EventHandler? Changed;

        public string BasePath { get; set; } = Path.Combine(Path.GetTempPath(), "InjectTests", "bases");
        public AppSettings Current { get; set; } = new();
        public string? CaptionFontPath => Current.CaptionFontPath;
        public string SdPath => Current.SdPath ?? string.Empty;
        public string OutputPath { get; set; } = Path.Combine(Path.GetTempPath(), "InjectTests", "out");
        public string WorkPath { get; set; } = Path.Combine(Path.GetTempPath(), "InjectTests", "work");

        public void Update(Func<AppSettings, AppSettings> change)
        {
            Current = change(Current);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    internal sealed class InjectBaseService : IBaseService
    {
        public List<BaseTitle> Bases { get; } = new();
        public HashSet<TitleId> Keyed { get; } = new();
        public Dictionary<TitleId, BaseStatus> Statuses { get; } = new();

        public InjectBaseService Add(BaseTitle @base, BaseStatus status = BaseStatus.Present)
        {
            Bases.Add(@base);
            Statuses[@base.TitleId] = status;
            return this;
        }

        public void AddCustom(BaseTitle @base) => Bases.Add(@base);

        public IReadOnlyList<BaseTitle> Available(SourceConsole console) => Bases.Where(b => b.Console == console).ToList();

        public Task<TitleDirectory> ImportAsync(BaseTitle @base, string sourceDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TitleDirectory(Path.Combine(Path.GetTempPath(), @base.TitleId.ToString())));

        public bool RemoveCustom(TitleId titleId) => Bases.RemoveAll(b => b.TitleId == titleId) > 0;

        public Task<TitleDirectory> DownloadAsync(BaseTitle @base, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool HasTitleKey(BaseTitle @base) => Keyed.Contains(@base.TitleId);

        public BaseStatus Status(BaseTitle @base) => Statuses.TryGetValue(@base.TitleId, out var status) ? status : BaseStatus.Downloadable;
    }

    internal sealed class RecordingInjectionService : IInjectionService
    {
        public byte[]? IconTga { get; set; }
        public Action? OnStart { get; set; }
        public Exception? Throws { get; set; }
        public Injection? Received { get; private set; }
        public string? OutputDirectory { get; private set; }
        public string? WorkDirectory { get; private set; }
        public List<InjectionProgress> Reports { get; } = new();

        public IReadOnlyList<BaseIssue> InspectBase(BaseTitle @base) => Array.Empty<BaseIssue>();

        public async Task<InjectedTitle> InjectAsync(Injection injection, string workDirectory, string outputDirectory, IProgress<InjectionProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            Received = injection;
            WorkDirectory = workDirectory;
            OutputDirectory = outputDirectory;
            OnStart?.Invoke();
            foreach (var report in Reports)
                progress?.Report(report);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Throws is not null)
                throw Throws;
            return new InjectedTitle(injection.Game, outputDirectory) { IconTga = IconTga };
        }
    }

    internal sealed class RecordingInjectionServiceFactory : IInjectionServiceFactory
    {
        public int Created { get; private set; }
        public Func<SourceConsole, string?, IReadOnlyList<string>> Missing { get; set; } = (_, _) => Array.Empty<string>();
        public RecordingInjectionService Service { get; } = new();

        public IInjectionService Create()
        {
            Created++;
            return Service;
        }

        public IReadOnlyList<string> MissingKeys(SourceConsole console, string? romPath = null) => Missing(console, romPath);

        public Func<BaseTitle, string, string?> Fit { get; set; } = (_, _) => null;

        public string? RomFit(BaseTitle @base, string romPath) => Fit(@base, romPath);
    }
}
