using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable options for one console; builds the domain options on demand.
/// </summary>
public abstract class ConsoleOptionsViewModel : ViewModelBase
{
    /// <summary>
    /// Creates a new instance of the <see cref="ConsoleOptionsViewModel"/> class.
    /// </summary>
    /// <param name="console">Console the options apply to.</param>
    protected ConsoleOptionsViewModel(SourceConsole console)
    {
        Console = console;
    }

    /// <summary>
    /// Console the options apply to.
    /// </summary>
    public SourceConsole Console { get; }

    /// <summary>
    /// The domain options as edited, or null when the console has none.
    /// </summary>
    public abstract IConsoleOptions? Build();
}
