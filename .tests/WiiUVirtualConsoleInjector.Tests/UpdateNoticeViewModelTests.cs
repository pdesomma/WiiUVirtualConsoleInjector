using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class UpdateNoticeViewModelTests
{
    private static readonly Uri Page = new("https://github.com/x/y/releases/tag/v1.1.0");
    private static readonly DateTimeOffset Start = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    private FakeUpdateCheck _check = null!;
    private FakeLinkOpener _links = null!;
    private DateTimeOffset _now;
    private FakeSettingsService _settings = null!;

    [TestInitialize]
    public void Initialize()
    {
        _check = new FakeUpdateCheck();
        _links = new FakeLinkOpener();
        _settings = new FakeSettingsService();
        _now = Start;
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateNoticeViewModel(null!, _settings, _links, new Version(1, 0)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateNoticeViewModel(_check, null!, _links, new Version(1, 0)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateNoticeViewModel(_check, _settings, null!, new Version(1, 0)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UpdateNoticeViewModel(_check, _settings, _links, null!));
    }

    [TestMethod]
    public async Task CheckAtStartupAsync_NewerRelease_ShowsItAndRemembersTheTime()
    {
        _check.Latest = new AppRelease(new Version(1, 1, 0), Page);
        var vm = Create();

        await vm.CheckAtStartupAsync();

        Assert.IsTrue(vm.IsAvailable);
        Assert.AreEqual("Version 1.1.0 is out", vm.Text);
        Assert.AreEqual(Start, _settings.Current.LastUpdateCheck);
        Assert.IsFalse(vm.IsChecking);
    }

    [TestMethod]
    public async Task CheckAtStartupAsync_SameOrOlderRelease_StaysQuiet()
    {
        _check.Latest = new AppRelease(new Version(1, 0, 0), Page);
        var vm = Create();

        await vm.CheckAtStartupAsync();

        Assert.IsFalse(vm.IsAvailable);
        Assert.IsNull(vm.Status);
        Assert.AreEqual("", vm.Text);
    }

    [TestMethod]
    public async Task CheckAtStartupAsync_TurnedOffOrRecent_DoesNotAsk()
    {
        _settings.Current = new AppSettings { CheckForUpdates = false };
        await Create().CheckAtStartupAsync();
        Assert.AreEqual(0, _check.Calls);

        _settings.Current = new AppSettings { LastUpdateCheck = Start - TimeSpan.FromHours(3) };
        await Create().CheckAtStartupAsync();
        Assert.AreEqual(0, _check.Calls);

        _settings.Current = new AppSettings { LastUpdateCheck = Start - TimeSpan.FromDays(2) };
        await Create().CheckAtStartupAsync();
        Assert.AreEqual(1, _check.Calls);
    }

    [TestMethod]
    public async Task CheckAtStartupAsync_Failure_IsSwallowed()
    {
        _check.Failure = new HttpRequestException("offline");
        var vm = Create();

        await vm.CheckAtStartupAsync();

        Assert.IsFalse(vm.IsAvailable);
        Assert.IsNull(vm.Status);
    }

    [TestMethod]
    public async Task CheckNow_IgnoresThrottleAndReportsEveryOutcome()
    {
        _settings.Current = new AppSettings { CheckForUpdates = false, LastUpdateCheck = Start };
        var vm = Create();

        await vm.CheckNowCommand.ExecuteAsync(null);
        Assert.AreEqual("Nothing has been published yet.", vm.Status);

        _check.Latest = new AppRelease(new Version(1, 0), Page);
        await vm.CheckNowCommand.ExecuteAsync(null);
        Assert.AreEqual("This is the newest version.", vm.Status);

        _check.Latest = new AppRelease(new Version(2, 0), Page);
        await vm.CheckNowCommand.ExecuteAsync(null);
        Assert.AreEqual("Version 2.0 is out.", vm.Status);
        Assert.IsTrue(vm.IsAvailable);

        _check.Failure = new HttpRequestException("offline");
        await vm.CheckNowCommand.ExecuteAsync(null);
        StringAssert.Contains(vm.Status, "offline");
        Assert.AreEqual(4, _check.Calls);
    }

    [TestMethod]
    public async Task Open_And_Dismiss_UseTheRelease()
    {
        _check.Latest = new AppRelease(new Version(1, 2, 3), Page);
        var vm = Create();
        await vm.CheckNowCommand.ExecuteAsync(null);

        await vm.OpenCommand.ExecuteAsync(null);
        CollectionAssert.AreEqual(new[] { Page }, _links.Opened);

        vm.DismissCommand.Execute(null);
        Assert.IsFalse(vm.IsAvailable);
        await vm.OpenCommand.ExecuteAsync(null);
        Assert.AreEqual(1, _links.Opened.Count);
    }

    [TestMethod]
    public void CheckAtStartup_Toggled_Persists()
    {
        var vm = Create();
        Assert.IsTrue(vm.CheckAtStartup);

        vm.CheckAtStartup = false;

        Assert.IsFalse(_settings.Current.CheckForUpdates);
        Assert.AreEqual("Version 1.0.0", vm.CurrentText);
    }

    private UpdateNoticeViewModel Create() => new(_check, _settings, _links, new Version(1, 0, 0, 0), () => _now);
}
