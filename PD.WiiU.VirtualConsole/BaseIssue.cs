namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Something a base title lacks or has wrong.
/// </summary>
/// <param name="Path">Path relative to the title root, forward slashes.</param>
/// <param name="Message">What is wrong.</param>
public sealed record BaseIssue(string Path, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Path}: {Message}";
}
