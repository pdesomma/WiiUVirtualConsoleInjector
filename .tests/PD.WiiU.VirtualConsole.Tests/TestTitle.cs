using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

/// <summary>
/// Builds throwaway base folders with the XML the metadata step needs.
/// </summary>
internal static class TestTitle
{
    public const string AppXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><version type=\"unsignedInt\" length=\"4\">16</version><title_id type=\"hexBinary\" length=\"8\">0005000010101D00</title_id><group_id type=\"hexBinary\" length=\"4\">00000000</group_id><title_version type=\"hexBinary\" length=\"2\">0000</title_version></app>";
    public const string MetaXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><menu type=\"complex\" access=\"777\"><title_id type=\"hexBinary\" length=\"8\">0005000010101D00</title_id><group_id type=\"hexBinary\" length=\"4\">00000000</group_id><product_code type=\"string\" length=\"32\">WUP-P-AAAA</product_code><company_code type=\"string\" length=\"8\">0001</company_code><title_version type=\"unsignedShort\" length=\"2\">0</title_version><region type=\"hexBinary\" length=\"4\">00000000</region><drc_use type=\"unsignedInt\" length=\"4\">0</drc_use><reserved_flag2 type=\"hexBinary\" length=\"4\">00000000</reserved_flag2><longname_ja type=\"string\" length=\"512\"></longname_ja><shortname_ja type=\"string\" length=\"256\"></shortname_ja><longname_en type=\"string\" length=\"512\"></longname_en><shortname_en type=\"string\" length=\"256\"></shortname_en><longname_fr type=\"string\" length=\"512\"></longname_fr><shortname_fr type=\"string\" length=\"256\"></shortname_fr><longname_de type=\"string\" length=\"512\"></longname_de><shortname_de type=\"string\" length=\"256\"></shortname_de><longname_it type=\"string\" length=\"512\"></longname_it><shortname_it type=\"string\" length=\"256\"></shortname_it><longname_es type=\"string\" length=\"512\"></longname_es><shortname_es type=\"string\" length=\"256\"></shortname_es><longname_zhs type=\"string\" length=\"512\"></longname_zhs><shortname_zhs type=\"string\" length=\"256\"></shortname_zhs><longname_ko type=\"string\" length=\"512\"></longname_ko><shortname_ko type=\"string\" length=\"256\"></shortname_ko><longname_nl type=\"string\" length=\"512\"></longname_nl><shortname_nl type=\"string\" length=\"256\"></shortname_nl><longname_pt type=\"string\" length=\"512\"></longname_pt><shortname_pt type=\"string\" length=\"256\"></shortname_pt><longname_ru type=\"string\" length=\"512\"></longname_ru><shortname_ru type=\"string\" length=\"256\"></shortname_ru><longname_zht type=\"string\" length=\"512\"></longname_zht><shortname_zht type=\"string\" length=\"256\"></shortname_zht></menu>";

    public static BaseTitle Base(SourceConsole console = SourceConsole.N64) =>
        new(new TitleId(TitleType.Game, 0x101C9300), "Test Base", Region.UnitedStates, console);

    public static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"))
        {
            Names = new Dictionary<Language, LocalizedName> { [Language.English] = new("Test", "Test Game") },
        };

    public static TitleDirectory Populate(string root)
    {
        var title = TitleDirectory.Create(root);
        File.WriteAllText(title.AppXmlPath, AppXml);
        File.WriteAllText(title.MetaXmlPath, MetaXml);
        File.WriteAllBytes(Path.Combine(title.Content, "data.bin"), new byte[] { 1, 2, 3 });
        return title;
    }

    public static string TempRoot() =>
        Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", Guid.NewGuid().ToString("N"));
}
