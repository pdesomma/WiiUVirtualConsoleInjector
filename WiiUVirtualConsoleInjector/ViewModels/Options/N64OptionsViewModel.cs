using CommunityToolkit.Mvvm.ComponentModel;
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
        : base(SourceConsole.N64)
    {
        Ini = new PathFieldViewModel(dialogs, "Emulator INI", "empty writes a blank one", new FileFilter("INI files", "*.ini"));
    }

    /// <summary>
    /// Emulator INI to ship.
    /// </summary>
    public PathFieldViewModel Ini { get; }

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
