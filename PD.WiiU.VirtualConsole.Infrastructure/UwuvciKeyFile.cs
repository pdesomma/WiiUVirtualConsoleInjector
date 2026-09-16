using System.Formats.Nrbf;
using System.IO.Compression;
using System.Runtime.Serialization;
using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// The previous application's bin/keys/&lt;console&gt;.vck: a gzip over a BinaryFormatter List of TKeys, each a base with its title key. Read with the safe NRBF decoder, so nothing in the file can run.
/// </summary>
public static class UwuvciKeyFile
{
    /// <summary>
    /// File extension.
    /// </summary>
    public const string Extension = ".vck";

    /// <summary>
    /// Every entry with a title ID and a well-formed key; anything else in the file is skipped.
    /// </summary>
    /// <param name="path">The .vck file.</param>
    /// <exception cref="InvalidDataException">Not a gzip, or not the expected serialized shape.</exception>
    public static IReadOnlyList<LegacyTitleKey> Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        byte[] bytes;
        try
        {
            using var file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var buffer = new MemoryStream();
            gzip.CopyTo(buffer);
            bytes = buffer.ToArray();
        }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} is not a gzip file.", e);
        }
        return Parse(bytes);
    }

    /// <summary>
    /// Reads the decompressed serialized list.
    /// </summary>
    /// <param name="bytes">NRBF payload.</param>
    /// <exception cref="InvalidDataException">Not the expected serialized shape.</exception>
    public static IReadOnlyList<LegacyTitleKey> Parse(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        SerializationRecord root;
        try
        {
            root = NrbfDecoder.Decode(new MemoryStream(bytes));
        }
        catch (Exception e) when (e is SerializationException or NotSupportedException or EndOfStreamException or IOException)
        {
            throw new InvalidDataException("Not a serialized key list.", e);
        }
        if (root is not ClassRecord list || !list.HasMember("_items") || list.GetArrayRecord("_items") is not SZArrayRecord<SerializationRecord?> items)
            throw new InvalidDataException("Not a serialized key list.");

        var result = new List<LegacyTitleKey>();
        foreach (var item in items.GetArray())
        {
            if (item is not ClassRecord entry || !entry.HasMember("<Tkey>k__BackingField") || !entry.HasMember("<Base>k__BackingField"))
                continue;
            var hex = entry.GetString("<Tkey>k__BackingField");
            if (entry.GetClassRecord("<Base>k__BackingField") is not { } @base || !@base.HasMember("<Tid>k__BackingField"))
                continue;
            if (!TitleId.TryParse(@base.GetString("<Tid>k__BackingField"), out var id) || string.IsNullOrWhiteSpace(hex))
                continue;
            try
            {
                result.Add(new LegacyTitleKey(id, EncryptedTitleKey.Parse(hex!.Trim())));
            }
            catch (FormatException)
            {
            }
        }
        return result;
    }
}
