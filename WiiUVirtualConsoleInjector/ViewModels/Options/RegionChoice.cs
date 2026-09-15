using WiiUSharp;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// One entry of the target-region list; a null region leaves the disc alone.
/// </summary>
/// <param name="Label">Text shown in the list.</param>
/// <param name="Region">Region to patch to, or null for unchanged.</param>
public sealed record RegionChoice(string Label, Region? Region)
{
    /// <summary>
    /// Every choice offered, unchanged first.
    /// </summary>
    public static readonly IReadOnlyList<RegionChoice> All = new[]
    {
        new RegionChoice("Unchanged", null),
        new RegionChoice("Japan", WiiUSharp.Region.Japan),
        new RegionChoice("United States", WiiUSharp.Region.UnitedStates),
        new RegionChoice("Europe", WiiUSharp.Region.Europe),
    };

    /// <inheritdoc/>
    public override string ToString() => Label;
}
