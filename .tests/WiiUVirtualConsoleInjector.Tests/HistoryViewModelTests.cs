using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class HistoryViewModelTests
{
    private InjectBaseService _bases = null!;
    private InjectDialogService _dialogs = null!;
    private FakeInjectionHistory _history = null!;
    private InjectViewModel _inject = null!;
    private readonly NavigationService _navigation = new();
    private FakeUiScheduler _ui = null!;
    private readonly List<Type> _shown = new();

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        _bases = new InjectBaseService();
        _dialogs = new InjectDialogService();
        _history = new FakeInjectionHistory();
        _ui = new FakeUiScheduler();
        foreach (var console in Enum.GetValues<SourceConsole>())
            _bases.Add(Base(console, 0x1000 + (uint)console, console + " Base"));
        var settings = new InjectSettingsService();
        _inject = new InjectViewModel(_bases, _dialogs, new RecordingInjectionServiceFactory(), settings, _navigation, new FakeSdCard(), new ArtworkBuilderViewModel(new FakeArtworkComposer(), _dialogs, () => settings.WorkPath), new FakeSoundPlayer(), _history, new FakeCompatibilityLists(), new FakeCommunityArtwork());
        _navigation.Requested += (_, type) => _shown.Add(type);
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new HistoryViewModel(null!, _inject, _navigation, _dialogs, _ui));
        Assert.ThrowsExactly<ArgumentNullException>(() => new HistoryViewModel(_history, null!, _navigation, _dialogs, _ui));
        Assert.ThrowsExactly<ArgumentNullException>(() => new HistoryViewModel(_history, _inject, null!, _dialogs, _ui));
        Assert.ThrowsExactly<ArgumentNullException>(() => new HistoryViewModel(_history, _inject, _navigation, null!, _ui));
        Assert.ThrowsExactly<ArgumentNullException>(() => new HistoryViewModel(_history, _inject, _navigation, _dialogs, null!));
    }

    [TestMethod]
    public void Constructor_EmptyHistory_IsEmptyWithOnePage()
    {
        var vm = Create();

        Assert.AreEqual("History", vm.Title);
        Assert.IsTrue(vm.IsEmpty);
        Assert.AreEqual(1, vm.PageCount);
        Assert.AreEqual(1, vm.Pages.Count);
        Assert.AreEqual("0 titles", vm.PageLabel);
        Assert.IsFalse(vm.CanGoNext);
        Assert.IsFalse(vm.CanGoPrevious);
        Assert.AreEqual(0, vm.Current.Count);
    }

    [TestMethod]
    public void Constructor_ThirteenRecords_PagesByTwelveNewestFirst()
    {
        for (var i = 0; i < 13; i++)
            _history.Records.Add(Record("r" + i, "Game " + i));

        var vm = Create();

        Assert.AreEqual(2, vm.PageCount);
        Assert.AreEqual(2, vm.Pages.Count);
        Assert.AreEqual("Page 1 of 2", vm.PageLabel);
        Assert.AreEqual(12, vm.Current.Count);
        Assert.AreEqual("Game 0", vm.Current[0].Name);
        Assert.IsTrue(vm.CanGoNext);
        Assert.IsFalse(vm.CanGoPrevious);
        Assert.AreEqual(1, vm.SelectedPage!.Number);
    }

    [TestMethod]
    public void NextPageCommand_SecondPage_ShowsTheRemainderAndDisablesNext()
    {
        for (var i = 0; i < 13; i++)
            _history.Records.Add(Record("r" + i, "Game " + i));
        var vm = Create();

        vm.NextPageCommand.Execute(null);

        Assert.AreEqual(2, vm.Page);
        Assert.AreEqual(1, vm.Current.Count);
        Assert.AreEqual("Game 12", vm.Current[0].Name);
        Assert.IsFalse(vm.NextPageCommand.CanExecute(null));
        Assert.IsTrue(vm.PreviousPageCommand.CanExecute(null));
        Assert.AreEqual("Page 2 of 2", vm.PageLabel);
    }

    [TestMethod]
    public void SelectedPage_SetFromDot_TurnsToThatPage()
    {
        for (var i = 0; i < 25; i++)
            _history.Records.Add(Record("r" + i, "Game " + i));
        var vm = Create();

        vm.SelectedPage = vm.Pages[2];

        Assert.AreEqual(3, vm.Page);
        Assert.AreEqual("Game 24", vm.Current.Single().Name);
    }

    [TestMethod]
    public async Task LoadCommand_Entry_FillsTheWizardAndShowsIt()
    {
        var record = Record("r1", "Game 1");
        _history.Records.Add(record);
        var vm = Create();

        await vm.LoadCommand.ExecuteAsync(vm.Current[0]);

        Assert.AreEqual("Game 1", _inject.Name);
        Assert.AreEqual(InjectViewModel.Steps.Count, _inject.Step);
        CollectionAssert.Contains(_shown, typeof(InjectViewModel));
        Assert.AreEqual(1, _dialogs.Infos.Count, "the fake ROM path does not exist, so the user is told");
        StringAssert.Contains(_dialogs.Infos[0].Message, record.RomPath);
    }

    [TestMethod]
    public async Task LoadCommand_RomStillThere_SaysNothing()
    {
        var rom = Path.GetTempFileName();
        try
        {
            _history.Records.Add(Record("r1", "Game 1", rom));
            var vm = Create();

            await vm.LoadCommand.ExecuteAsync(vm.Current[0]);

            Assert.AreEqual(0, _dialogs.Infos.Count);
            Assert.AreEqual(rom, _inject.RomPath);
        }
        finally
        {
            File.Delete(rom);
        }
    }

    [TestMethod]
    public async Task LoadCommand_Null_DoesNothing()
    {
        var vm = Create();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _shown.Count);
    }

    [TestMethod]
    public async Task ForgetCommand_Confirmed_RemovesTheRecordAndRefreshes()
    {
        _history.Records.Add(Record("r1", "Game 1"));
        _dialogs.ConfirmResult = true;
        var vm = Create();

        await vm.ForgetCommand.ExecuteAsync(vm.Current[0]);

        CollectionAssert.AreEqual(new[] { "r1" }, _history.Removed);
        Assert.IsTrue(vm.IsEmpty);
    }

    [TestMethod]
    public async Task ForgetCommand_Declined_KeepsTheRecord()
    {
        _history.Records.Add(Record("r1", "Game 1"));
        _dialogs.ConfirmResult = false;
        var vm = Create();

        await vm.ForgetCommand.ExecuteAsync(vm.Current[0]);

        Assert.AreEqual(0, _history.Removed.Count);
        Assert.AreEqual(1, vm.Current.Count);
    }

    [TestMethod]
    public void HistoryChanged_Raised_ReloadsOnTheUiThread()
    {
        var vm = Create();
        Assert.IsTrue(vm.IsEmpty);

        _history.Records.Add(Record("r1", "Game 1"));
        _history.RaiseChanged();

        Assert.AreEqual(1, _ui.Posted);
        Assert.IsFalse(vm.IsEmpty);
        Assert.AreEqual("1 title", vm.PageLabel);
    }

    [TestMethod]
    public void Reload_FewerPagesThanBefore_ClampsToTheLastPage()
    {
        for (var i = 0; i < 13; i++)
            _history.Records.Add(Record("r" + i, "Game " + i));
        var vm = Create();
        vm.Page = 2;

        _history.Records.RemoveAt(12);
        vm.Reload();

        Assert.AreEqual(1, vm.Page);
        Assert.AreEqual(12, vm.Current.Count);
    }

    [TestMethod]
    public void Entry_Record_DescribesItself()
    {
        var record = Record("r1", "Long Name, Second Line");

        var entry = new HistoryEntryViewModel(record);

        Assert.AreEqual("Long Name", entry.Name);
        Assert.AreEqual(SourceConsole.Nes, entry.Console);
        Assert.AreEqual("NES", entry.ConsoleName);
        Assert.IsTrue(entry.IsRomMissing);
        Assert.IsFalse(entry.HasIcon);
        Assert.IsNull(entry.IconPath);
        StringAssert.Contains(entry.Tooltip, "Long Name / Second Line");
        StringAssert.Contains(entry.Tooltip, "WUP-N-ABCD");
        StringAssert.Contains(entry.Tooltip, "ROM not found");
        Assert.ThrowsExactly<ArgumentNullException>(() => new HistoryEntryViewModel(null!));
    }

    [TestMethod]
    public async Task EntryCommands_OnTheTile_LoadAndForgetThroughThePage()
    {
        _history.Records.Add(Record("r1", "Game 1"));
        _dialogs.ConfirmResult = true;
        var vm = Create();
        var entry = vm.Current[0];

        Assert.IsTrue(entry.LoadCommand.CanExecute(null));
        Assert.IsTrue(entry.ForgetCommand.CanExecute(null));
        await entry.LoadCommand.ExecuteAsync(null);
        Assert.AreEqual("Game 1", _inject.Name);
        CollectionAssert.Contains(_shown, typeof(InjectViewModel));

        await entry.ForgetCommand.ExecuteAsync(null);
        CollectionAssert.AreEqual(new[] { "r1" }, _history.Removed);
        Assert.IsTrue(vm.IsEmpty);
    }

    [TestMethod]
    public async Task EntryCommands_Standalone_DoNothing()
    {
        var entry = new HistoryEntryViewModel(Record("r1", "Game 1"));

        await entry.LoadCommand.ExecuteAsync(null);
        await entry.ForgetCommand.ExecuteAsync(null);
    }

    [TestMethod]
    public void ArrowNavigation_Grid_TurnsPages()
    {
        for (var i = 0; i < 13; i++)
            _history.Records.Add(Record("r" + i, "Game " + i));
        IArrowNavigation vm = Create();

        Assert.AreEqual("Next page", vm.NextHint);
        Assert.AreEqual("Previous page", vm.PreviousHint);
        Assert.IsTrue(vm.NextCommand.CanExecute(null));
        Assert.IsFalse(vm.PreviousCommand.CanExecute(null));

        vm.NextCommand.Execute(null);

        Assert.AreEqual(2, ((HistoryViewModel)vm).Page);
    }

    private HistoryViewModel Create() => new(_history, _inject, _navigation, _dialogs, _ui);

    private static InjectionRecord Record(string id, string name, string rom = @"C:\nowhere\game.nes") =>
        new(id, DateTimeOffset.Now, SourceConsole.Nes, new TitleId(TitleType.Game, 0x1000 + (uint)SourceConsole.Nes), rom, name,
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "ABCD")));
}
