using WiiUVirtualConsoleInjector.Assets;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class AcknowledgementsViewModelTests
{
    [TestMethod]
    public void Constructor_NullOpener_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new AcknowledgementsViewModel(null!));
    }

    [TestMethod]
    public void Constructor_Always_IsTheCreditsPageWithAGlyph()
    {
        var vm = new AcknowledgementsViewModel(new FakeLinkOpener());

        Assert.AreEqual("Credits", vm.Title);
        Assert.IsTrue(vm.HasNavGlyph);
        Assert.AreSame(Acknowledgements.Shipped, vm.Shipped);
        Assert.AreSame(Acknowledgements.Borrowed, vm.Borrowed);
    }

    [TestMethod]
    public void Acknowledgements_EveryEntry_HasNameAuthorsAndRole()
    {
        foreach (var entry in Acknowledgements.Shipped.Concat(Acknowledgements.Borrowed))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Name));
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Authors), entry.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Role), entry.Name);
            Assert.AreEqual(entry.Url is not null, entry.HasUrl, entry.Name);
            Assert.AreEqual(entry.License is not null, entry.HasLicense, entry.Name);
            if (entry.Url is { } url)
                Assert.IsTrue(url.IsAbsoluteUri && url.Scheme == Uri.UriSchemeHttps, entry.Name);
        }
    }

    [TestMethod]
    public void Acknowledgements_Shipped_AllCarryALicenseAndCoverTheEmbeddedBinaries()
    {
        var names = Acknowledgements.Shipped.Select(a => a.Name).ToArray();

        Assert.IsTrue(Acknowledgements.Shipped.All(a => a.HasLicense && a.HasUrl));
        CollectionAssert.Contains(names, "Goomba Color");
        CollectionAssert.Contains(names, "Nintendont autoboot forwarder");
        CollectionAssert.Contains(names, "wiivc_chan_booter");
        CollectionAssert.Contains(names, "Nunito");
    }

    [TestMethod]
    public void Acknowledgements_Borrowed_LeadsWithTheOriginalApp()
    {
        Assert.AreEqual("UWUVCI AIO", Acknowledgements.Borrowed[0].Name);
        Assert.IsTrue(Acknowledgements.Borrowed.Count >= 15);
    }

    [TestMethod]
    public async Task OpenCommand_EntryWithUrl_OpensIt()
    {
        var opener = new FakeLinkOpener();
        var vm = new AcknowledgementsViewModel(opener);
        var entry = Acknowledgements.Shipped[0];

        await vm.OpenCommand.ExecuteAsync(entry);

        CollectionAssert.AreEqual(new[] { entry.Url }, opener.Opened);
    }

    [TestMethod]
    public async Task OpenCommand_EntryWithoutUrlOrNull_OpensNothing()
    {
        var opener = new FakeLinkOpener();
        var vm = new AcknowledgementsViewModel(opener);

        await vm.OpenCommand.ExecuteAsync(new Acknowledgement("x", "y", "z"));
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.AreEqual(0, opener.Opened.Count);
    }
}
