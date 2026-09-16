using System.Net.Http;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Nintendont from the Wii U GamePad fork on GitHub, the build the previous application installed: loader.dol as boot.dol, plus meta.xml and icon.png for the Homebrew Channel.
/// </summary>
public sealed class GitHubNintendontSource : INintendontSource
{
    /// <summary>
    /// Raw-file root of the fork.
    /// </summary>
    public const string DefaultBaseUrl = "https://raw.githubusercontent.com/GaryOderNichts/Nintendont/master/";
    /// <summary>
    /// Folder under the card root.
    /// </summary>
    public const string AppFolder = "apps/nintendont";

    private static readonly (string Source, string Target)[] Files =
    {
        ("loader/loader.dol", "boot.dol"),
        ("nintendont/meta.xml", "meta.xml"),
        ("nintendont/icon.png", "icon.png"),
    };

    private readonly HttpClient _client;

    /// <summary>
    /// Creates a new instance of the <see cref="GitHubNintendontSource"/> class.
    /// </summary>
    /// <param name="client">Shared client.</param>
    /// <param name="baseUrl">Raw-file root, or null for the fork.</param>
    public GitHubNintendontSource(HttpClient client, string? baseUrl = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        BaseUrl = baseUrl ?? DefaultBaseUrl;
        if (!BaseUrl.EndsWith("/", StringComparison.Ordinal))
            BaseUrl += "/";
    }

    /// <summary>
    /// Raw-file root in use.
    /// </summary>
    public string BaseUrl { get; }

    /// <inheritdoc/>
    public async Task InstallAsync(string cardRoot, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cardRoot))
            throw new ArgumentException("Card root is required.", nameof(cardRoot));

        var folder = Path.Combine(cardRoot, AppFolder.Replace('/', Path.DirectorySeparatorChar));
        var staging = folder + ".download";
        if (Directory.Exists(staging))
            Directory.Delete(staging, recursive: true);
        Directory.CreateDirectory(staging);
        try
        {
            foreach (var (source, target) in Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report("Downloading " + target);
                using var response = await _client.GetAsync(new Uri(BaseUrl + source), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var file = new FileStream(Path.Combine(staging, target), FileMode.Create, FileAccess.Write, FileShare.None);
                await content.CopyToAsync(file, 81920, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            Directory.Delete(staging, recursive: true);
            throw;
        }

        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.Move(staging, folder);
    }
}
