using WiiUVirtualConsoleInjector.ViewModels;
using WiiUVirtualConsoleInjector.ViewModels.Options;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ViewLocatorTests
{
    [TestMethod]
    public void EveryPageAndOptionsViewModel_HasAViewOfTheMatchingName()
    {
        var assembly = typeof(ViewModelBase).Assembly;
        var viewModels = assembly.GetTypes()
            .Where(t => !t.IsAbstract && (typeof(PageViewModel).IsAssignableFrom(t) || typeof(ConsoleOptionsViewModel).IsAssignableFrom(t)))
            .ToArray();

        Assert.IsTrue(viewModels.Length >= 12, string.Join(", ", viewModels.Select(t => t.Name)));
        foreach (var viewModel in viewModels)
        {
            var viewName = viewModel.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            Assert.IsNotNull(assembly.GetType(viewName), viewName);
        }
    }
}
