using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Tells what a folder offered as a base is and what title it holds, from its own files.
/// </summary>
public static class BaseFolder
{
    /// <summary>
    /// Reads the folder: a package by its title.tmd, a title by its code/content/meta; identity from the TMD or the XML files, whichever is there.
    /// </summary>
    /// <param name="folder">Folder to read.</param>
    /// <returns>Null when the folder is neither.</returns>
    public static BaseFolderInfo? Inspect(string folder)
    {
        if (folder is null)
            throw new ArgumentNullException(nameof(folder));
        if (!Directory.Exists(folder))
            return null;

        var tmdPath = Path.Combine(folder, NusFormat.TmdFileName);
        if (File.Exists(tmdPath))
        {
            TitleId? id = null;
            try
            {
                id = Tmd.Parse(File.ReadAllBytes(tmdPath)).Title.TitleId;
            }
            catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException)
            {
            }
            return new BaseFolderInfo(BaseFolderKind.Package, id, null, null);
        }

        var title = new TitleDirectory(folder);
        if (!title.Exists)
            return null;

        TitleId? titleId = null;
        string? name = null;
        Region? region = null;
        try
        {
            if (File.Exists(title.AppXmlPath))
                titleId = AppXml.Load(title.AppXmlPath).ReadTitleId();
            if (File.Exists(title.MetaXmlPath))
            {
                // read the few elements wanted one by one; a trimmed meta.xml need not hold every language
                var meta = MetaXml.Load(title.MetaXmlPath).Document.Root;
                if (titleId is null && meta?.Element("title_id")?.Value is { } id && TitleId.TryParse(id, out var parsed))
                    titleId = parsed;
                name = meta?.Element("longname_en")?.Value;
                if (string.IsNullOrWhiteSpace(name))
                    name = meta?.Elements().FirstOrDefault(e => e.Name.LocalName.StartsWith("longname_", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(e.Value))?.Value;
                if (meta?.Element("region")?.Value is { } hex && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var value))
                    region = (Region)value is Region.Japan or Region.Europe or Region.UnitedStates ? (Region)value : null;
            }
        }
        catch (Exception e) when (e is IOException or InvalidDataException or FormatException or ArgumentException or System.Xml.XmlException)
        {
        }
        return new BaseFolderInfo(BaseFolderKind.Title, titleId, string.IsNullOrWhiteSpace(name) ? null : name!.Replace('\n', ' ').Trim(), region);
    }
}
