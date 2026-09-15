namespace PD.WiiU.VirtualConsole;

/// <summary>
/// How far a base download is.
/// </summary>
/// <param name="Phase">Fetching files or unpacking them.</param>
/// <param name="Item">File being fetched or content being unpacked.</param>
/// <param name="ItemNumber">One-based position of the item.</param>
/// <param name="ItemCount">Items in the phase; 0 when unknown.</param>
/// <param name="BytesReceived">Bytes of the item so far; 0 while unpacking.</param>
/// <param name="BytesTotal">Item length when known.</param>
public sealed record BaseDownloadProgress(BaseDownloadPhase Phase, string Item, int ItemNumber, int ItemCount, long BytesReceived, long? BytesTotal);
