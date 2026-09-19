using WiiUSharp;

namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class RetroArchTitleConfigTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = TestPaths.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Settings_Always_TurnOffSaveOnExitAndTheStartUpNotifications()
    {
        var keys = RetroArchTitleConfig.Settings.Select(s => s.Key).ToArray();

        Assert.IsTrue(RetroArchTitleConfig.Settings.All(s => s.Value == "false"));
        CollectionAssert.Contains(keys, "config_save_on_exit");
        CollectionAssert.Contains(keys, "notification_show_autoconfig");
        CollectionAssert.Contains(keys, "notification_show_config_override_load");
        CollectionAssert.Contains(keys, "notification_show_remap_load");
        CollectionAssert.DoesNotContain(keys, "video_font_enable", "in-game messages such as a saved state stay");
        CollectionAssert.DoesNotContain(keys, "notification_show_save_state");
        Assert.AreEqual(keys.Length, keys.Distinct().Count());
    }

    [TestMethod]
    public void Text_Always_IsOneQuotedAssignmentPerLineWithLf()
    {
        var text = RetroArchTitleConfig.Text;

        StringAssert.StartsWith(text, "config_save_on_exit = \"false\"\n");
        Assert.IsFalse(text.Contains('\r'));
        Assert.AreEqual(RetroArchTitleConfig.Settings.Count, text.Split('\n').Length - 1);
        Assert.IsTrue(text.Split('\n').Where(l => l.Length > 0).All(l => l.EndsWith(" = \"false\"", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Write_NullTitle_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => RetroArchTitleConfig.Write(null!));
    }

    [TestMethod]
    public void Write_StagedTitle_PutsTheFileUnderContent()
    {
        var title = new TitleDirectory(_root);
        TitleDirectory.Create(_root);

        var path = RetroArchTitleConfig.Write(title);

        Assert.AreEqual(Path.Combine(title.Content, "retroarch.cfg"), path);
        Assert.AreEqual(RetroArchTitleConfig.Text, File.ReadAllText(path));
    }
}
