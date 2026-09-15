namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One page of the history grid, for the pager dots.
/// </summary>
/// <param name="Number">1-based page number.</param>
/// <param name="Label">Dot tooltip.</param>
public sealed record HistoryPage(int Number, string Label);
