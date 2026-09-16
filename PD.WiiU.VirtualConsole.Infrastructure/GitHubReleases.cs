using System.Net;
using System.Net.Http;
using System.Text.Json;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// The newest release of a GitHub repository, read from the public API without a token; tags are v-prefixed version numbers.
/// </summary>
public sealed class GitHubReleases : IUpdateCheck
{
    /// <summary>
    /// The application's own repository.
    /// </summary>
    public const string DefaultRepository = "pdesomma/WiiUVirtualConsoleInjector";

    private readonly HttpClient _client;

    /// <summary>
    /// Creates a new instance of the <see cref="GitHubReleases"/> class.
    /// </summary>
    /// <param name="client">Shared client.</param>
    /// <param name="repository">owner/name, or null for the application's.</param>
    /// <param name="apiBase">API root, or null for api.github.com.</param>
    public GitHubReleases(HttpClient client, string? repository = null, Uri? apiBase = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        Repository = repository ?? DefaultRepository;
        if (Repository.Count(c => c == '/') != 1)
            throw new ArgumentException("Repository is owner/name.", nameof(repository));
        LatestUri = new Uri(apiBase ?? new Uri("https://api.github.com/"), $"repos/{Repository}/releases/latest");
    }

    /// <summary>
    /// The endpoint asked.
    /// </summary>
    public Uri LatestUri { get; }
    /// <summary>
    /// owner/name in use.
    /// </summary>
    public string Repository { get; }

    /// <summary>
    /// Reads a release tag such as v1.2.3 or 1.2; anything else is null.
    /// </summary>
    /// <param name="tag">Tag name.</param>
    public static Version? ParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;
        var text = tag!.Trim();
        if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            text = text.Substring(1);
        return Version.TryParse(text, out var version) ? version : null;
    }

    /// <inheritdoc/>
    public async Task<AppRelease?> LatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestUri);
        request.Headers.TryAddWithoutValidation("User-Agent", "WiiUVirtualConsoleInjector");
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return null;
        var version = root.TryGetProperty("tag_name", out var tag) ? ParseTag(tag.GetString()) : null;
        if (version is null)
            return null;
        var page = root.TryGetProperty("html_url", out var url) && Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri) ? uri : new Uri($"https://github.com/{Repository}/releases/latest");
        return new AppRelease(version, page);
    }
}
