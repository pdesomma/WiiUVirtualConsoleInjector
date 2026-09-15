using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector;

/// <summary>
/// Maps a view model to the view of the same name with "View" in place of "ViewModel".
/// </summary>
[RequiresUnreferencedCode("Resolves views by name through reflection.")]
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc/>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);
        return type is null ? new TextBlock { Text = "Not found: " + name } : (Control)Activator.CreateInstance(type)!;
    }

    /// <inheritdoc/>
    public bool Match(object? data) => data is ViewModelBase;
}
