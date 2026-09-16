using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="N64Options"/>.
/// </summary>
public sealed partial class N64OptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private bool _removeDarkFilter;
    [ObservableProperty]
    private bool _wideScreen;

    /// <summary>
    /// Creates a new instance of the <see cref="N64OptionsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker for the INI.</param>
    public N64OptionsViewModel(IDialogService dialogs)
        : this(dialogs, () => Path.GetTempPath())
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="N64OptionsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker for the INI.</param>
    /// <param name="workFolder">Where a written INI goes.</param>
    public N64OptionsViewModel(IDialogService dialogs, Func<string> workFolder)
        : base(SourceConsole.N64)
    {
        Ini = new PathFieldViewModel(dialogs, "Emulator INI", "empty writes a blank one", new FileFilter("INI files", "*.ini"));
        IniBuilder = new N64IniBuilderViewModel(dialogs, workFolder);
        IniBuilder.Written += (_, path) => Ini.Path = path;
        Ini.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PathFieldViewModel.Path))
            {
                OnPropertyChanged(nameof(HasIni));
                EditIniCommand.NotifyCanExecuteChanged();
            }
        };
    }

    /// <summary>
    /// Emulator INI to ship.
    /// </summary>
    public PathFieldViewModel Ini { get; }

    /// <summary>
    /// Writes or edits the INI from settings.
    /// </summary>
    public N64IniBuilderViewModel IniBuilder { get; }

    /// <summary>
    /// Reads the picked INI into the builder's fields.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasIni))]
    private Task EditIniAsync() => IniBuilder.LoadFileAsync(Ini.Path!);

    /// <summary>
    /// True when an INI is picked.
    /// </summary>
    public bool HasIni => !string.IsNullOrWhiteSpace(Ini.Path);

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new N64Options
    {
        IniPath = Ini.Path,
        RemoveDarkFilter = RemoveDarkFilter,
        WideScreen = WideScreen,
    };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options)
    {
        var n64 = options as N64Options;
        Ini.Path = n64?.IniPath;
        RemoveDarkFilter = n64?.RemoveDarkFilter ?? false;
        WideScreen = n64?.WideScreen ?? false;
    }
}
