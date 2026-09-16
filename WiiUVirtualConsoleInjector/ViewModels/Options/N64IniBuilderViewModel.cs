using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Writes an N64 emulator INI from a few settings, or loads one to edit; the result lands in the work folder and becomes the pick.
/// </summary>
public sealed partial class N64IniBuilderViewModel : ViewModelBase
{
    /// <summary>
    /// Shown for a size left to the emulator.
    /// </summary>
    public const string AutoSize = "Auto";

    private readonly IDialogService _dialogs;
    private readonly Func<string> _workFolder;

    [ObservableProperty]
    private N64BackupType? _backupType;
    [ObservableProperty]
    private string _backupSize = AutoSize;
    [ObservableProperty]
    private string? _comment;
    [ObservableProperty]
    private bool _expansionPak = true;
    [ObservableProperty]
    private string _extra = string.Empty;
    [ObservableProperty]
    private bool _isOpen;
    [ObservableProperty]
    private bool _retraceByVsync = true;
    [ObservableProperty]
    private bool _rspMultiCore;
    [ObservableProperty]
    private bool _rumble = true;
    [ObservableProperty]
    private string? _status;
    [ObservableProperty]
    private bool _useTimer = true;

    /// <summary>
    /// Creates a new instance of the <see cref="N64IniBuilderViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Error dialogs.</param>
    /// <param name="workFolder">Where written files go.</param>
    public N64IniBuilderViewModel(IDialogService dialogs, Func<string> workFolder)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
    }

    /// <summary>
    /// Raised with the path of a freshly written file.
    /// </summary>
    public event EventHandler<string>? Written;

    /// <summary>
    /// Save sizes to pick from.
    /// </summary>
    public IReadOnlyList<string> BackupSizes { get; } = new[] { AutoSize, "512", "2048", "32768", "131072" };
    /// <summary>
    /// Save types to pick from; null is "let the emulator detect".
    /// </summary>
    public IReadOnlyList<N64BackupType?> BackupTypes { get; } = new N64BackupType?[] { null, N64BackupType.Eeprom, N64BackupType.Sram, N64BackupType.Flash, N64BackupType.Auto };

    /// <summary>
    /// The settings as an INI.
    /// </summary>
    public N64Ini Build() => new()
    {
        Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment,
        BackupType = BackupType,
        BackupSize = int.TryParse(BackupSize, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var size) && size > 0 ? size : null,
        // the flags are written either way, as the previous creator did; multi-core only when asked for
        RetraceByVsync = RetraceByVsync,
        Rumble = Rumble,
        UseTimer = UseTimer,
        RspMultiCore = RspMultiCore ? true : null,
        ExpansionPak = ExpansionPak,
        Extra = Extra,
    };

    /// <summary>
    /// Fills the fields from an INI's settings.
    /// </summary>
    /// <param name="ini">Parsed file.</param>
    public void Load(N64Ini ini)
    {
        if (ini is null)
            throw new ArgumentNullException(nameof(ini));

        Comment = ini.Comment;
        BackupType = ini.BackupType;
        BackupSize = ini.BackupSize?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? AutoSize;
        RetraceByVsync = ini.RetraceByVsync ?? true;
        Rumble = ini.Rumble ?? true;
        UseTimer = ini.UseTimer ?? true;
        RspMultiCore = ini.RspMultiCore ?? false;
        ExpansionPak = ini.ExpansionPak ?? true;
        Extra = ini.Extra;
    }

    /// <summary>
    /// Reads an existing INI into the fields.
    /// </summary>
    /// <param name="path">File to read.</param>
    /// <returns>False when it could not be read.</returns>
    public async Task<bool> LoadFileAsync(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        try
        {
            Load(N64Ini.Parse(File.ReadAllText(path)));
            Status = "Loaded " + Path.GetFileName(path) + ".";
            IsOpen = true;
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowErrorAsync("Emulator INI", e.Message);
            return false;
        }
    }

    /// <summary>
    /// Writes the INI into the work folder and offers it as the pick.
    /// </summary>
    [RelayCommand]
    private async Task WriteAsync()
    {
        var folder = Path.Combine(_workFolder(), "n64-ini", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "game.ini");
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, Build().ToText());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowErrorAsync("Emulator INI", e.Message);
            return;
        }
        Status = "Written; it is the INI for this inject now.";
        Written?.Invoke(this, path);
    }
}
