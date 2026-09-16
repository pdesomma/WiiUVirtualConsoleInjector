using System.Net;
using System.Net.Http;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// The UWUVCI-IMAGES repository on GitHub, read through raw file URLs: a folder is a hit when it has an iconTex in one of the image formats, and the other files are looked for beside it.
/// </summary>
public sealed class GitHubCommunityArtwork : ICommunityArtwork
{
    /// <summary>
    /// Raw-file root of the repository.
    /// </summary>
    public const string DefaultBaseUrl = "https://raw.githubusercontent.com/UWUVCI-PRIME/UWUVCI-IMAGES/master/";
    /// <summary>
    /// Image formats a folder may use, in the order they are tried.
    /// </summary>
    public static readonly IReadOnlyList<string> Extensions = new[] { "png", "jpg", "jpeg", "tga" };

    private readonly HttpClient _client;

    /// <summary>
    /// Creates a new instance of the <see cref="GitHubCommunityArtwork"/> class.
    /// </summary>
    /// <param name="client">Shared client.</param>
    /// <param name="baseUrl">Raw-file root, or null for the real repository.</param>
    public GitHubCommunityArtwork(HttpClient client, string? baseUrl = null)
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
    public async Task DownloadAsync(Uri source, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination is required.", nameof(destinationPath));

        using var response = await _client.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        using var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var file = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, 81920, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<CommunityArtworkHit?> FindAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids is null)
            throw new ArgumentNullException(nameof(ids));

        foreach (var id in ids)
        {
            foreach (var extension in Extensions)
            {
                var icon = Url(id, "iconTex." + extension);
                if (!await ExistsAsync(icon, cancellationToken).ConfigureAwait(false))
                    continue;

                // the guidelines want icon and TV in the same format; a folder without the TV is not a usable hit
                var bootTv = Url(id, "bootTvTex." + extension);
                if (!await ExistsAsync(bootTv, cancellationToken).ConfigureAwait(false))
                    continue;

                var bootDrc = Url(id, "bootDrcTex." + extension);
                var gameIni = Url(id, "game.ini");
                var bootSound = Url(id, "BootSound.btsnd");
                return new CommunityArtworkHit(
                    id,
                    icon,
                    bootTv,
                    await ExistsAsync(bootDrc, cancellationToken).ConfigureAwait(false) ? bootDrc : null,
                    id.StartsWith("n64/", StringComparison.Ordinal) && await ExistsAsync(gameIni, cancellationToken).ConfigureAwait(false) ? gameIni : null,
                    await ExistsAsync(bootSound, cancellationToken).ConfigureAwait(false) ? bootSound : null);
            }
        }
        return null;
    }

    private async Task<bool> ExistsAsync(Uri url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    private Uri Url(string id, string file) => new(BaseUrl + id.Trim('/') + "/" + file);
}
