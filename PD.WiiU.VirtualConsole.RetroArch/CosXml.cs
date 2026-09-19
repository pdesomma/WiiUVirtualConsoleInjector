using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// The launch parameters in code/cos.xml; only the argument string is touched.
/// </summary>
public sealed class CosXml
{
    /// <summary>
    /// Element holding the command line the loader hands the executable.
    /// </summary>
    public const string ArgumentsElement = "argstr";
    /// <summary>
    /// File name under code.
    /// </summary>
    public const string FileName = "cos.xml";
    /// <summary>
    /// Root element name.
    /// </summary>
    public const string RootName = "app";

    private readonly XDocument _document;

    private CosXml(XDocument document)
    {
        if (document.Root?.Name.LocalName != RootName)
            throw new InvalidDataException($"Expected an <{RootName}> document.");
        _document = document;
    }

    /// <summary>
    /// The command line: executable name first, then its arguments, split on spaces by the loader.
    /// </summary>
    public string Arguments
    {
        get => Element(ArgumentsElement).Value;
        set => Element(ArgumentsElement).Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Reads a cos.xml.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <exception cref="InvalidDataException">Not a cos.xml.</exception>
    public static CosXml Load(string path)
    {
        using var stream = File.OpenRead(path);
        return new CosXml(XDocument.Load(stream, LoadOptions.PreserveWhitespace));
    }

    /// <summary>
    /// Writes the file with its declaration, without a byte order mark.
    /// </summary>
    /// <param name="path">Destination.</param>
    public void Save(string path)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            NewLineHandling = NewLineHandling.None,
        };
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = XmlWriter.Create(stream, settings);
        _document.Save(writer);
    }

    private XElement Element(string name) =>
        _document.Root!.Element(name) ?? throw new InvalidDataException($"cos.xml has no <{name}> element.");
}
