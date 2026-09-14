using WiiSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Bridges WiiSharp's disc crypto to the NFS container's partition port.
/// </summary>
public sealed class WiiPartitionCipher : IPartitionCipher
{
    private readonly DiscCipher _cipher;

    /// <summary>
    /// Creates a new instance of the <see cref="WiiPartitionCipher"/> class.
    /// </summary>
    /// <param name="commonKey">Key that unwraps each partition's title key.</param>
    public WiiPartitionCipher(CommonKey commonKey)
    {
        _cipher = new DiscCipher(commonKey);
    }

    /// <inheritdoc/>
    public DiscDataSpan Decrypt(Stream iso, Stream payload, CancellationToken cancellationToken)
    {
        var range = _cipher.Decrypt(iso, payload, cancellationToken);
        return new DiscDataSpan(range.Start, range.Length);
    }

    /// <inheritdoc/>
    public void Encrypt(Stream payload, Stream iso, CancellationToken cancellationToken) =>
        _cipher.Encrypt(payload, iso, cancellationToken);
}
