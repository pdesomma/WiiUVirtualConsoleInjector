namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One page of the inject wizard.
/// </summary>
/// <param name="Number">One-based position.</param>
/// <param name="Label">Name shown under the step dots.</param>
public sealed record WizardStep(int Number, string Label);
