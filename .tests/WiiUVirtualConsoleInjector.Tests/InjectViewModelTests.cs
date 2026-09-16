using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;
using WiiUVirtualConsoleInjector.ViewModels;
using WiiUVirtualConsoleInjector.ViewModels.Options;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectViewModelTests
{
    private InjectBaseService _bases = null!;
    private InjectDialogService _dialogs = null!;
    private RecordingInjectionServiceFactory _factory = null!;
    private FakeSdCard _sdCard = null!;
    private FakeSoundPlayer _sounds = null!;
    private FakeInjectionHistory _history = null!;
    private FakeCompatibilityLists _compatibility = null!;
    private FakeCommunityArtwork _community = null!;
    private InjectSettingsService _settings = null!;
    private readonly NavigationService _navigation = new();

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        _bases = new InjectBaseService();
        _dialogs = new InjectDialogService();
        _factory = new RecordingInjectionServiceFactory();
        _settings = new InjectSettingsService();
        _sdCard = new FakeSdCard();
        _sounds = new FakeSoundPlayer();
        _history = new FakeInjectionHistory();
        _compatibility = new FakeCompatibilityLists();
        _community = new FakeCommunityArtwork();
        foreach (var console in Enum.GetValues<SourceConsole>())
            _bases.Add(Base(console, 0x1000 + (uint)console, console + " Base"));
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(null!, _dialogs, _factory, _settings, _navigation, _sdCard, Builder(), _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, null!, _factory, _settings, _navigation, _sdCard, Builder(), _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, null!, _settings, _navigation, _sdCard, Builder(), _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, null!, _navigation, _sdCard, Builder(), _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, null!, _sdCard, Builder(), _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, _navigation, null!, Builder(), _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, _navigation, _sdCard, null!, _sounds, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, _navigation, _sdCard, Builder(), null!, _history, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, _navigation, _sdCard, Builder(), _sounds, null!, _compatibility, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, _navigation, _sdCard, Builder(), _sounds, _history, null!, _community));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectViewModel(_bases, _dialogs, _factory, _settings, _navigation, _sdCard, Builder(), _sounds, _history, _compatibility, null!));
    }

    [TestMethod]
    public void Constructor_Default_StartsOnNesWithItsBases()
    {
        var vm = Create();

        Assert.AreEqual("Inject", vm.Title);
        Assert.AreEqual(SourceConsole.Nes, vm.SelectedConsole);
        Assert.IsInstanceOfType<NesOptionsViewModel>(vm.CurrentOptions);
        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual("Nes Base [UnitedStates]", vm.SelectedBase!.Display);
        Assert.IsNull(vm.BaseHint);
        Assert.IsFalse(vm.IsGamePadVisible);
        Assert.IsFalse(vm.IsTurboCd);
        Assert.AreEqual(Enum.GetValues<SourceConsole>().Length, vm.Consoles.Count);
    }

    [TestMethod]
    public void Bases_TitleKeyStored_ShowsOnTheChoice()
    {
        var keyed = Base(SourceConsole.Nes, 0x2000, "Keyed");
        _bases.Add(keyed);
        _bases.Keyed.Add(keyed.TitleId);

        var vm = Create();

        Assert.IsTrue(vm.Bases.Single(b => b.Base.TitleId.Equals(keyed.TitleId)).HasTitleKey);
        Assert.IsFalse(vm.Bases.First(b => !b.Base.TitleId.Equals(keyed.TitleId)).HasTitleKey, "present or not, the glyph is about the key");
    }

    [TestMethod]
    public void SelectedConsole_Changed_SwapsOptionsReloadsBasesAndClearsRom()
    {
        var vm = Ready();

        vm.SelectedConsole = SourceConsole.Wii;

        Assert.IsInstanceOfType<WiiOptionsViewModel>(vm.CurrentOptions);
        Assert.IsNull(vm.RomPath);
        Assert.AreEqual(SourceConsole.Wii, vm.SelectedBase!.Base.Console);
        Assert.IsTrue(vm.IsGamePadVisible);
        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public void SelectedConsole_EveryConsole_GetsMatchingOptionsViewModel()
    {
        var vm = Create();
        var expected = new Dictionary<SourceConsole, Type>
        {
            [SourceConsole.Nes] = typeof(NesOptionsViewModel),
            [SourceConsole.Snes] = typeof(SnesOptionsViewModel),
            [SourceConsole.N64] = typeof(N64OptionsViewModel),
            [SourceConsole.Gba] = typeof(GbaOptionsViewModel),
            [SourceConsole.Nds] = typeof(NdsOptionsViewModel),
            [SourceConsole.Tg16] = typeof(NoOptionsViewModel),
            [SourceConsole.Msx] = typeof(NoOptionsViewModel),
            [SourceConsole.Wii] = typeof(WiiOptionsViewModel),
            [SourceConsole.GameCube] = typeof(GameCubeOptionsViewModel),
        };

        foreach (var (console, type) in expected)
        {
            vm.SelectedConsole = console;
            Assert.AreEqual(type, vm.CurrentOptions.GetType(), console.ToString());
            Assert.AreEqual(console, vm.CurrentOptions.Console);
            Assert.AreEqual(console == SourceConsole.Tg16, vm.IsTurboCd);
        }
    }

    [TestMethod]
    public void CanInject_BaseNotPresent_FalseWithHint()
    {
        _bases.Statuses[_bases.Bases[0].TitleId] = BaseStatus.Downloadable;
        var vm = Ready();

        Assert.IsFalse(vm.CanInject);
        Assert.IsFalse(vm.SelectedBase!.IsPresent);
        StringAssert.Contains(vm.SelectedBase.Display, "(not downloaded)");
        StringAssert.Contains(vm.BaseHint, "Bases & Keys");
        Assert.IsFalse(vm.InjectCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanInject_NoBaseForConsole_FalseWithHint()
    {
        _bases.Bases.RemoveAll(b => b.Console == SourceConsole.Nes);
        var vm = Ready();

        Assert.IsNull(vm.SelectedBase);
        Assert.IsFalse(vm.CanInject);
        StringAssert.Contains(vm.BaseHint, "No base");
    }

    [TestMethod]
    public void CanInject_NoRom_False()
    {
        var vm = Ready();
        vm.RomPath = " ";

        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public void CanInject_BlankName_False()
    {
        var vm = Ready();
        vm.Name = "  ";

        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public void CanInject_MissingKeys_FalseWithHint()
    {
        _factory.Missing = (c, rom) => c == SourceConsole.Wii && rom is not null ? new[] { "Wii U common key", "Wii common key" } : Array.Empty<string>();
        var vm = Ready();
        Assert.IsTrue(vm.CanInject);
        Assert.IsFalse(vm.HasMissingKeys);
        Assert.IsNull(vm.MissingKeysHint);

        vm.SelectedConsole = SourceConsole.Wii;
        Assert.IsFalse(vm.HasMissingKeys, "nothing to complain about until the ROM is known");
        vm.RomPath = @"C:\game.iso";

        Assert.IsFalse(vm.CanInject);
        Assert.IsTrue(vm.HasMissingKeys);
        StringAssert.Contains(vm.MissingKeysHint, "Wii U common key and Wii common key");

        vm.RomPath = null;
        Assert.IsFalse(vm.HasMissingKeys, "recomputed as the ROM changes");
    }

    [TestMethod]
    public void CanInject_ProductIdNotFourCharacters_False()
    {
        var vm = Ready();

        vm.ProductId = "ABC";
        Assert.IsFalse(vm.CanInject);
        vm.ProductId = "ABCD";
        Assert.IsTrue(vm.CanInject);
        vm.ProductId = "";
        Assert.IsTrue(vm.CanInject);
    }

    [TestMethod]
    public async Task CanInject_WhileRunning_False()
    {
        var vm = Ready();
        var duringRun = true;
        var cancelDuringRun = false;
        _factory.Service.OnStart = () =>
        {
            duringRun = vm.CanInject;
            cancelDuringRun = vm.CancelCommand.CanExecute(null);
        };

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.IsFalse(duringRun);
        Assert.IsTrue(cancelDuringRun);
        Assert.IsFalse(vm.IsRunning);
        Assert.IsFalse(vm.CancelCommand.CanExecute(null));
        Assert.AreEqual(1, vm.Step, "a success starts the wizard over");
    }

    [TestMethod]
    public void CanInject_EverythingSet_True()
    {
        var vm = Ready();

        Assert.IsTrue(vm.CanInject);
        Assert.IsTrue(vm.InjectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ActivateAsync_StatusChanged_RefreshesKeepingSelection()
    {
        var second = Base(SourceConsole.Nes, 0x2000, "Second");
        _bases.Add(second, BaseStatus.NeedsTitleKey);
        var vm = Ready();
        vm.SelectedBase = vm.Bases[1];
        Assert.IsFalse(vm.CanInject);

        _bases.Statuses[second.TitleId] = BaseStatus.Present;
        await vm.ActivateAsync();

        Assert.AreEqual(second.TitleId, vm.SelectedBase!.Base.TitleId);
        Assert.IsTrue(vm.SelectedBase.IsPresent);
        Assert.IsTrue(vm.CanInject);
    }

    [TestMethod]
    public void Ready_RecommendedBasePresent_IsPickedOverEarlierOnes()
    {
        _bases.Add(Base(SourceConsole.Nes, 0x2000, "Plain"));
        _bases.Add(new BaseTitle(Base(SourceConsole.Nes, 0x3000, "Best").TitleId, "Best", Region.Japan, SourceConsole.Nes) { IsRecommended = true });

        var vm = Ready();

        Assert.AreEqual("Best", vm.SelectedBase!.Base.Name);
    }

    [TestMethod]
    public void Ready_RecommendedBaseNotDownloaded_PresentOneWinsButNotOverNothing()
    {
        _bases.Add(new BaseTitle(Base(SourceConsole.Nes, 0x3000, "Best").TitleId, "Best", Region.Japan, SourceConsole.Nes) { IsRecommended = true }, BaseStatus.Downloadable);
        Assert.AreEqual("Nes Base", Ready().SelectedBase!.Base.Name);

        _bases.Statuses[_bases.Bases[0].TitleId] = BaseStatus.Downloadable;
        Assert.AreEqual("Best", Ready().SelectedBase!.Base.Name);
    }

    [TestMethod]
    public async Task ActivateAsync_SelectionGone_PrefersFirstPresentBase()
    {
        _bases.Add(Base(SourceConsole.Nes, 0x2000, "Second"), BaseStatus.Downloadable);
        _bases.Add(Base(SourceConsole.Nes, 0x3000, "Third"));
        _bases.Statuses[_bases.Bases[0].TitleId] = BaseStatus.Downloadable;
        var vm = Ready();
        Assert.AreEqual("Third", vm.SelectedBase!.Base.Name);

        _bases.Bases.RemoveAll(b => b.Name == "Third");
        await vm.ActivateAsync();

        Assert.AreEqual("Nes Base", vm.SelectedBase!.Base.Name);
        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public async Task Inject_SnesWarningNotSuppressed_AsksFirst()
    {
        var vm = Ready(SourceConsole.Snes, @"C:\game.sfc");

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _dialogs.Confirms.Count);
        Assert.AreEqual(InjectViewModel.WarningTitle, _dialogs.Confirms[0].Title);
        StringAssert.Contains(_dialogs.Confirms[0].Message, "Co-Processors");
        Assert.IsNotNull(_factory.Service.Received);
    }

    [TestMethod]
    public async Task Inject_NdsWarningSuppressed_SkipsConfirm()
    {
        _settings.Current = new AppSettings().Suppress(InjectionWarning.NdsDsiEnhanced);
        var vm = Ready(SourceConsole.Nds, @"C:\game.nds");

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _dialogs.Confirms.Count);
        Assert.IsNotNull(_factory.Service.Received);
    }

    [TestMethod]
    public async Task RomFit_RomTooBigForBase_BlocksInjectAndSaysSo()
    {
        _factory.Fit = (b, r) => b.Console == SourceConsole.Nes ? $"{Path.GetFileName(r)} is 384 KB; this base holds 64 KB." : null;
        var vm = Ready();
        await vm.RomFitCheck;

        Assert.IsFalse(vm.CanInject);
        Assert.IsTrue(vm.HasRomFitHint);
        StringAssert.Contains(vm.RomFitHint, "game.nes is 384 KB");
        Assert.IsNull(vm.BaseHint, "the base itself is fine");

        _factory.Fit = (_, _) => null;
        vm.RomPath = @"C:\small.nes";
        await vm.RomFitCheck;

        Assert.IsFalse(vm.HasRomFitHint);
        Assert.IsTrue(vm.CanInject);
    }

    [TestMethod]
    public async Task OpenSlot_EmptyTilePicks_FilledTileShowsThePicture()
    {
        var vm = Ready();
        _dialogs.NextPaths.Enqueue(@"C:\art\icon.png");

        await vm.OpenSlotCommand.ExecuteAsync(vm.Icon);
        Assert.AreEqual(@"C:\art\icon.png", vm.Icon.Path);
        Assert.AreEqual(0, _dialogs.Images.Count);

        await vm.OpenSlotCommand.ExecuteAsync(vm.Icon);
        Assert.AreEqual(1, _dialogs.Images.Count);
        Assert.AreEqual((vm.Icon.Label, @"C:\art\icon.png"), _dialogs.Images[0]);

        await vm.OpenSlotCommand.ExecuteAsync(null);
        await vm.ShowImageCommand.ExecuteAsync(" ");
        await vm.ShowImageCommand.ExecuteAsync(@"C:\art\tv.png");
        Assert.AreEqual(2, _dialogs.Images.Count);
        Assert.AreEqual("Community artwork", _dialogs.Images[1].Title);
    }

    [TestMethod]
    public async Task RomFit_BaseNotDownloaded_IsNotAsked()
    {
        var asked = 0;
        _factory.Fit = (_, _) => { asked++; return "no"; };
        _bases.Statuses[_bases.Bases[0].TitleId] = BaseStatus.Downloadable;
        var vm = Ready();
        await vm.RomFitCheck;

        Assert.AreEqual(0, asked);
        StringAssert.Contains(vm.BaseHint, "not downloaded");
    }

    [TestMethod]
    public async Task Inject_WarningDeclined_Aborts()
    {
        _dialogs.ConfirmResult = false;
        var vm = Ready(SourceConsole.Nds, @"C:\game.nds");

        await vm.InjectCommand.ExecuteAsync(null);

        StringAssert.Contains(_dialogs.Confirms[0].Message, "DSi Enhanced");
        Assert.IsNull(_factory.Service.Received);
        Assert.AreEqual(0, _factory.Created);
    }

    [TestMethod]
    public async Task Inject_GameCubeGcz_AsksButIsoDoesNot()
    {
        var vm = Ready(SourceConsole.GameCube, @"C:\game.GCZ");
        await vm.InjectCommand.ExecuteAsync(null);
        Assert.AreEqual(1, _dialogs.Confirms.Count);
        StringAssert.Contains(_dialogs.Confirms[0].Message, "GCZ");

        vm.RomPath = @"C:\game.iso";
        await vm.InjectCommand.ExecuteAsync(null);
        Assert.AreEqual(1, _dialogs.Confirms.Count);
    }

    [TestMethod]
    public async Task Inject_Nes_NoWarning()
    {
        var vm = Ready();

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _dialogs.Confirms.Count);
    }

    [TestMethod]
    public async Task Inject_BuildsInjectionFromForm()
    {
        var vm = Ready();
        vm.Name = "Super Game,The Sequel";
        vm.Icon.Path = @"C:\art\icon.png";
        vm.BootTv.Path = @"C:\art\tv.png";
        vm.BootDrc.Path = @"C:\art\drc.png";
        vm.BootLogo.Path = @"C:\art\logo.png";
        vm.BootSound.Path = @"C:\art\boot.wav";
        vm.GamePad = true;
        ((NesOptionsViewModel)vm.CurrentOptions).PixelPerfect = true;

        await vm.InjectCommand.ExecuteAsync(null);

        var injection = _factory.Service.Received!;
        Assert.AreSame(vm.SelectedBase!.Base, injection.Base);
        Assert.AreEqual(@"C:\game.nes", injection.Rom.Path);
        Assert.AreEqual(SourceConsole.Nes, injection.Rom.Console);
        Assert.IsTrue(((NesOptions)injection.Options!).PixelPerfect);
        Assert.AreEqual(@"C:\art\icon.png", injection.Artwork.Icon);
        Assert.AreEqual(@"C:\art\tv.png", injection.Artwork.BootTv);
        Assert.AreEqual(@"C:\art\drc.png", injection.Artwork.BootDrc);
        Assert.AreEqual(@"C:\art\logo.png", injection.Artwork.BootLogo);
        Assert.AreEqual(@"C:\art\boot.wav", injection.BootSoundPath);
        Assert.AreEqual("Super Game", injection.Game.Names[Language.English].ShortName);
        Assert.AreEqual("Super Game\nThe Sequel", injection.Game.Names[Language.English].LongName);
        Assert.IsNull(injection.Game.GamePadUse, "GamePad flag only applies to Wii and GameCube");
        Assert.AreEqual(_settings.OutputPath, _factory.Service.OutputDirectory);
    }

    [TestMethod]
    public async Task Inject_WiiWithGamePadAndProductId_MapsBoth()
    {
        var vm = Ready(SourceConsole.Wii, @"C:\game.wbfs");
        vm.GamePad = true;
        vm.ProductId = "ABCD";
        var options = (WiiOptionsViewModel)vm.CurrentOptions;
        options.TargetRegion = RegionChoice.All[3];
        options.LrPatch = true;

        await vm.InjectCommand.ExecuteAsync(null);

        var injection = _factory.Service.Received!;
        Assert.AreEqual(65537u, injection.Game.GamePadUse);
        Assert.AreEqual("ABCD", injection.Game.ProductCode.Id);
        var built = (WiiOptions)injection.Options!;
        Assert.AreEqual(Region.Europe, built.TargetRegion);
        Assert.IsTrue(built.LrPatch);
    }

    [TestMethod]
    public async Task Inject_Tg16_HasNoOptions()
    {
        var vm = Ready(SourceConsole.Tg16, @"C:\game.pce");

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.IsNull(_factory.Service.Received!.Options);
        Assert.IsNull(_factory.Service.Received.Artwork.Icon);
        Assert.IsNull(_factory.Service.Received.BootSoundPath);
    }

    [TestMethod]
    public async Task Inject_Success_LogsProgressAndShowsOutput()
    {
        _factory.Service.Reports.Add(new InjectionProgress(InjectionStep.StageBase, "Staging"));
        _factory.Service.Reports.Add(new InjectionProgress(InjectionStep.Pack, "Packing"));
        var vm = Ready();
        string? status = null;
        string? step = null;
        string[]? log = null;
        _dialogs.OnInfo = () => (status, step, log) = (vm.Status, vm.CurrentStep, vm.Log.ToArray());

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual("Done", status, "while the done box is up");
        Assert.AreEqual("Pack", step);
        CollectionAssert.AreEqual(new[] { "StageBase: Staging", "Pack: Packing" }, log);
        Assert.AreEqual(1, _dialogs.Infos.Count);
        StringAssert.Contains(_dialogs.Infos[0].Message, _settings.OutputPath);
        Assert.IsTrue(vm.HasOutputSize, "measured even when the fake wrote nothing");
        StringAssert.Contains(_dialogs.Infos[0].Message, "(" + vm.OutputSize!.Value.Text + ")");
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task Inject_InjectionException_ShowsStepAndMessage()
    {
        _factory.Service.Throws = new InjectionException(InjectionStep.InjectRom, "bad rom");
        var vm = Ready();

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual("Failed", vm.Status);
        Assert.AreEqual("Failed at InjectRom: bad rom", _dialogs.Errors.Single().Message);
        Assert.AreEqual(0, _dialogs.Infos.Count);
        Assert.IsFalse(vm.IsRunning);
    }

    [TestMethod]
    public async Task Inject_OtherException_ShowsMessage()
    {
        _factory.Service.Throws = new InvalidOperationException("no key");
        var vm = Ready();

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual("Failed", vm.Status);
        Assert.AreEqual("no key", _dialogs.Errors.Single().Message);
    }

    [TestMethod]
    public async Task Inject_Cancelled_SetsStatusWithoutDialogs()
    {
        var vm = Ready();
        _factory.Service.OnStart = () => vm.CancelCommand.Execute(null);

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual("Cancelled", vm.Status);
        Assert.AreEqual(0, _dialogs.Errors.Count);
        Assert.AreEqual(0, _dialogs.Infos.Count);
        Assert.IsFalse(vm.IsRunning);
    }

    [TestMethod]
    public async Task Inject_WorkFolder_UnderSettingsAndRemovedAfterwards()
    {
        _settings.WorkPath = Path.Combine(Path.GetTempPath(), "InjectTests", Guid.NewGuid().ToString("N"));
        var vm = Ready();
        _factory.Service.OnStart = () => Directory.CreateDirectory(_factory.Service.WorkDirectory!);

        await vm.InjectCommand.ExecuteAsync(null);

        StringAssert.StartsWith(_factory.Service.WorkDirectory, _settings.WorkPath);
        Assert.AreNotEqual(_settings.WorkPath, _factory.Service.WorkDirectory);
        Assert.IsFalse(Directory.Exists(_factory.Service.WorkDirectory));
        Directory.Delete(_settings.WorkPath, recursive: true);
    }

    [TestMethod]
    public async Task PickRom_PerConsole_UsesConsoleFilters()
    {
        var vm = Create();
        _dialogs.NextPaths.Enqueue(@"C:\game.nes");
        await vm.PickRomCommand.ExecuteAsync(null);
        Assert.AreEqual(@"C:\game.nes", vm.RomPath);
        CollectionAssert.AreEqual(new[] { "*.nes" }, _dialogs.FilePicks[0].Filters.Single().Patterns);

        vm.SelectedConsole = SourceConsole.Wii;
        _dialogs.NextPaths.Enqueue(@"C:\game.wad");
        await vm.PickRomCommand.ExecuteAsync(null);
        var patterns = _dialogs.FilePicks[1].Filters.SelectMany(f => f.Patterns).Distinct().ToArray();
        CollectionAssert.AreEquivalent(new[] { "*.iso", "*.wbfs", "*.dol", "*.wad" }, patterns);
    }

    [TestMethod]
    public async Task PickRom_Cancelled_KeepsPath()
    {
        var vm = Ready();

        await vm.PickRomCommand.ExecuteAsync(null);

        Assert.AreEqual(@"C:\game.nes", vm.RomPath);
    }

    [TestMethod]
    public async Task PickTurboCdFolder_Picked_SetsRomPathToFolder()
    {
        var vm = Create();
        vm.SelectedConsole = SourceConsole.Tg16;
        _dialogs.NextPaths.Enqueue(@"C:\games\ys");

        await vm.PickTurboCdFolderCommand.ExecuteAsync(null);

        Assert.AreEqual(@"C:\games\ys", vm.RomPath);
        Assert.AreEqual(1, _dialogs.FolderPicks.Count);
        Assert.AreEqual(0, _dialogs.FilePicks.Count);
    }

    private InjectViewModel Create() => new(_bases, _dialogs, _factory, _settings, _navigation, _sdCard, Builder(), _sounds, _history, _compatibility, _community);

    [TestMethod]
    public async Task CommunityArtwork_HitApplied_FillsTheSlotsAndTheN64Ini()
    {
        var root = Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var rom = Path.Combine(root, "game.z64");
            var bytes = new byte[0x100];
            System.Text.Encoding.ASCII.GetBytes("NGEE").CopyTo(bytes, 0x3B);
            File.WriteAllBytes(rom, bytes);
            _community.Hit = new CommunityArtworkHit("n64/NGEE", new Uri("https://r.test/n64/NGEE/iconTex.png"), new Uri("https://r.test/n64/NGEE/bootTvTex.png"), null, new Uri("https://r.test/n64/NGEE/game.ini"), new Uri("https://r.test/n64/NGEE/BootSound.btsnd"));
            var vm = Ready(SourceConsole.N64, rom);
            Assert.IsTrue(vm.CommunityArtwork.LookUpCommand.CanExecute(null));

            await vm.CommunityArtwork.LookUpCommand.ExecuteAsync(null);

            CollectionAssert.AreEqual(new[] { "n64/NGEE", "n64/NEGE" }, _community.Lookups.Single().ToArray());
            Assert.IsTrue(vm.CommunityArtwork.HasHit);
            StringAssert.Contains(vm.CommunityArtwork.Status, "n64/NGEE");
            StringAssert.Contains(vm.CommunityArtwork.Status, "emulator INI");
            Assert.AreEqual(4, _community.Downloaded.Count);
            Assert.IsTrue(File.Exists(vm.CommunityArtwork.Files!.Icon));
            StringAssert.Contains(vm.CommunityArtwork.Files.Icon, Path.Combine("community-artwork", "n64_NGEE"));

            vm.CommunityArtwork.ApplyCommand.Execute(null);

            Assert.IsFalse(vm.CommunityArtwork.HasHit);
            StringAssert.EndsWith(vm.Icon.Path, "iconTex.png");
            StringAssert.EndsWith(vm.BootTv.Path, "bootTvTex.png");
            Assert.IsNull(vm.BootDrc.Path, "no GamePad screen in that folder");
            StringAssert.EndsWith(vm.BootSound.Path, "BootSound.btsnd");
            StringAssert.EndsWith(((N64OptionsViewModel)vm.CurrentOptions).Ini.Path, "game.ini");

            vm.RomPath = null;
            Assert.IsNull(vm.CommunityArtwork.Status, "a new ROM forgets the last lookup");
            Assert.IsFalse(vm.CommunityArtwork.LookUpCommand.CanExecute(null));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task CommunityArtwork_MissOrNoCodeOrOffline_ExplainsItself()
    {
        var root = Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var nds = Path.Combine(root, "game.nds");
            var bytes = new byte[0x200];
            System.Text.Encoding.ASCII.GetBytes("ARPE").CopyTo(bytes, 0xC);
            File.WriteAllBytes(nds, bytes);
            var vm = Ready(SourceConsole.Nds, nds);

            await vm.CommunityArtwork.LookUpCommand.ExecuteAsync(null);
            StringAssert.Contains(vm.CommunityArtwork.Status, "Nothing in the community repository under nds/ARPE");
            Assert.IsFalse(vm.CommunityArtwork.HasHit);

            _community.Failure = new System.Net.Http.HttpRequestException("offline");
            await vm.CommunityArtwork.LookUpCommand.ExecuteAsync(null);
            StringAssert.Contains(vm.CommunityArtwork.Status, "could not be reached");

            var gb = Path.Combine(root, "game.gb");
            File.WriteAllBytes(gb, new byte[0x200]);
            vm.SelectedConsole = SourceConsole.Gba;
            vm.RomPath = gb;
            await vm.CommunityArtwork.LookUpCommand.ExecuteAsync(null);
            StringAssert.Contains(vm.CommunityArtwork.Status, "no code");
            Assert.AreEqual(2, _community.Lookups.Count, "nothing to ask with");

            vm.CommunityArtwork.DismissCommand.Execute(null);
            Assert.IsFalse(vm.CommunityArtwork.HasHit);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void RomPath_HeaderName_FillsABlankNameOnly()
    {
        var root = Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var iso = Path.Combine(root, "game.iso");
            var bytes = new byte[0x100];
            System.Text.Encoding.ASCII.GetBytes("LUIGI'S MANSION").CopyTo(bytes, 0x20);
            File.WriteAllBytes(iso, bytes);
            var vm = Create();
            vm.SelectedConsole = SourceConsole.GameCube;

            vm.RomPath = iso;
            Assert.AreEqual("Luigi's Mansion", vm.Name);

            vm.Name = "My Name";
            vm.RomPath = null;
            vm.RomPath = iso;
            Assert.AreEqual("My Name", vm.Name, "a typed name is not overwritten");

            vm.Name = "";
            vm.RomPath = Path.Combine(root, "missing.iso");
            Assert.AreEqual("", vm.Name, "nothing readable leaves it blank");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void CommunityArtworkViewModel_NullArguments_Throw()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new CommunityArtworkViewModel(null!, () => SourceConsole.Nes, () => null, () => "w"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new CommunityArtworkViewModel(_community, null!, () => null, () => "w"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new CommunityArtworkViewModel(_community, () => SourceConsole.Nes, null!, () => "w"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new CommunityArtworkViewModel(_community, () => SourceConsole.Nes, () => null, null!));
    }

    [TestMethod]
    public async Task OpenCompatibilityList_OpensThePageForTheSelectedConsole()
    {
        var vm = Ready(SourceConsole.N64, @"C:\game.z64");

        await vm.OpenCompatibilityListCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new[] { SourceConsole.N64 }, _compatibility.Opened);
    }

    [TestMethod]
    public async Task InjectCommand_CopyToSdCardOn_CopiesThePackedTitleAndReportsWhereItLanded()
    {
        var vm = Ready();
        _settings.Current = _settings.Current with { CopyToSdCard = true, SdPath = @"E:\" };
        string[]? log = null;
        string? status = null;
        _dialogs.OnInfo = () => (log, status) = (vm.Log.ToArray(), vm.Status);

        await vm.InjectCommand.ExecuteAsync(null);

        var copy = _sdCard.Copies.Single();
        Assert.AreEqual(_factory.Service.OutputDirectory, copy.Title);
        Assert.AreEqual(@"E:\", copy.Root);
        StringAssert.Contains(_dialogs.Infos.Single().Message, _sdCard.CopyResult);
        CollectionAssert.Contains(log!, "Copying title.tmd");
        Assert.AreEqual("Done", status);
    }

    [TestMethod]
    public async Task InjectCommand_CopyOffOrNoCard_LeavesTheCardAlone()
    {
        var vm = Ready();
        await vm.InjectCommand.ExecuteAsync(null);

        _settings.Current = _settings.Current with { CopyToSdCard = true };
        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _sdCard.Copies.Count);
        StringAssert.Contains(_dialogs.Infos[0].Message, _factory.Service.OutputDirectory!);
    }

    [TestMethod]
    public async Task InjectCommand_CopyFails_KeepsTheInjectAndSaysTheCopyFailed()
    {
        var vm = Ready();
        _settings.Current = _settings.Current with { CopyToSdCard = true, SdPath = @"E:\" };
        _sdCard.Failure = new IOException("card full");
        string? status = null;
        _dialogs.OnInfo = () => status = vm.Status;

        await vm.InjectCommand.ExecuteAsync(null);

        StringAssert.Contains(_dialogs.Errors.Single().Message, "card full");
        StringAssert.Contains(_dialogs.Infos.Single().Message, _factory.Service.OutputDirectory!);
        Assert.AreEqual("Done", status, "the inject still counts");
        Assert.AreEqual(1, vm.Step, "and the wizard starts over");
    }

    [TestMethod]
    public async Task PreviewSoundCommand_NoSound_IsOff()
    {
        var vm = Create();

        Assert.IsFalse(vm.CanPreviewSound);
        Assert.IsFalse(vm.PreviewSoundCommand.CanExecute(null));
        vm.BootSound.Path = @"C:\boot.wav";
        Assert.IsTrue(vm.PreviewSoundCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task PreviewSoundCommand_PlaysThenStopsThenFollowsTheEnd()
    {
        var vm = Create();
        vm.BootSound.Path = @"C:\boot.wav";

        await vm.PreviewSoundCommand.ExecuteAsync(null);
        Assert.IsTrue(vm.IsPlayingSound);
        CollectionAssert.AreEqual(new[] { @"C:\boot.wav" }, _sounds.Played);

        await vm.PreviewSoundCommand.ExecuteAsync(null);
        Assert.IsFalse(vm.IsPlayingSound, "second press stops");

        await vm.PreviewSoundCommand.ExecuteAsync(null);
        _sounds.Finish();
        Assert.IsFalse(vm.IsPlayingSound, "playback ending clears the state");
    }

    [TestMethod]
    public async Task PreviewSoundCommand_ChangingTheSound_StopsPlayback()
    {
        var vm = Create();
        vm.BootSound.Path = @"C:\boot.wav";
        await vm.PreviewSoundCommand.ExecuteAsync(null);

        vm.BootSound.Path = @"C:\other.wav";

        Assert.IsFalse(vm.IsPlayingSound);
    }

    [TestMethod]
    public async Task PreviewSoundCommand_PlayerFails_ShowsTheError()
    {
        var vm = Create();
        vm.BootSound.Path = @"C:\boot.wav";
        _sounds.Failure = new InvalidDataException("not audio");

        await vm.PreviewSoundCommand.ExecuteAsync(null);

        Assert.IsFalse(vm.IsPlayingSound);
        StringAssert.Contains(_dialogs.Errors.Single().Message, "not audio");
    }

    [TestMethod]
    public async Task Artwork_BuiltForOneSlot_FillsOnlyThatSlot()
    {
        var vm = Ready();
        vm.Step = 3;
        vm.ArtworkBuilder.Tv.SourcePath = @"C:\shot.png";

        await vm.ArtworkBuilder.Tv.BuildCommand.ExecuteAsync(null);

        StringAssert.EndsWith(vm.BootTv.Path!, "bootTvTex.png");
        Assert.IsNull(vm.Icon.Path);
        Assert.IsNull(vm.BootDrc.Path);
        Assert.IsNull(vm.BootLogo.Path);

        await vm.ArtworkBuilder.Logo.BuildCommand.ExecuteAsync(null);

        StringAssert.EndsWith(vm.BootLogo.Path!, "bootLogoTex.png");
        Assert.IsNull(vm.Icon.Path, "still untouched");
    }

    [TestMethod]
    public async Task Format_DefaultsToWupAndFlowsIntoTheInjection()
    {
        var vm = Ready();
        Assert.AreEqual(OutputFormat.Wup, vm.Format);
        StringAssert.Contains(vm.ReviewOutput, "install");
        CollectionAssert.AreEqual(new[] { OutputFormat.Wup, OutputFormat.Loadiine }, vm.Formats.ToArray());

        vm.Format = OutputFormat.Loadiine;
        StringAssert.Contains(vm.ReviewOutput, "wiiu/games");
        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(OutputFormat.Loadiine, _factory.Service.Received!.Format);
    }

    [TestMethod]
    public void Constructor_Default_OffersOverlaysForTheStartingConsoleWithoutAnyClick()
    {
        var vm = Create();

        Assert.IsTrue(vm.ArtworkBuilder.Icon.Overlays.Count > 0, "icon overlays");
        Assert.IsTrue(vm.ArtworkBuilder.Tv.Overlays.Count > 0, "tv overlays");
        Assert.IsTrue(vm.ArtworkBuilder.GamePad.Overlays.Count > 0, "gamepad overlays");
        Assert.IsTrue(vm.ArtworkBuilder.Logo.Overlays.Count > 0, "logo overlays");
        Assert.AreEqual("icon-nes-1", vm.ArtworkBuilder.Icon.Overlay!.Key);
        Assert.IsTrue(vm.ArtworkBuilder.Logo.CanBuild);

        vm.ArtworkBuilder.Icon.SourcePath = @"C:\shot.png";
        Assert.IsTrue(vm.ArtworkBuilder.Icon.BuildCommand.CanExecute(null), "a source image is enough on the starting console");
    }

    [TestMethod]
    public async Task InjectCommand_Succeeds_RemembersTheInjectWithItsIconAndIds()
    {
        var vm = Ready(SourceConsole.Snes, @"C:\game.sfc");
        vm.Name = "Super Game, The Sequel";
        vm.ShortName = "Super";
        vm.ProductId = "ABCD";
        vm.Format = OutputFormat.Loadiine;
        vm.Icon.Path = @"C:\icon.png";
        vm.BootSound.Path = @"C:\boot.wav";
        ((SnesOptionsViewModel)vm.CurrentOptions).PixelPerfect = true;
        _factory.Service.IconTga = new byte[] { 1, 2, 3 };

        await vm.InjectCommand.ExecuteAsync(null);

        var (record, icon) = _history.Added.Single();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, icon);
        Assert.AreEqual(SourceConsole.Snes, record.Console);
        Assert.AreEqual(_factory.Service.Received!.Base.TitleId, record.BaseTitleId);
        Assert.AreEqual(@"C:\game.sfc", record.RomPath);
        Assert.AreEqual("Super Game, The Sequel", record.Name);
        Assert.AreEqual("Super", record.ShortName);
        Assert.AreEqual("ABCD", record.ProductId);
        Assert.AreEqual(OutputFormat.Loadiine, record.Format);
        Assert.AreEqual(@"C:\icon.png", record.Artwork.Icon);
        Assert.AreEqual(@"C:\boot.wav", record.BootSoundPath);
        Assert.IsTrue(((SnesOptions)record.Options!).PixelPerfect);
        Assert.AreEqual(_factory.Service.OutputDirectory, record.OutputDirectory);
        Assert.AreEqual(TitleIdentity.Of(_factory.Service.Received.Game), record.Identity);
    }

    [TestMethod]
    public async Task InjectCommand_HistoryFails_StillCountsAsDoneAndSaysSo()
    {
        var vm = Ready();
        _history.AddError = new IOException("disk full");

        await vm.InjectCommand.ExecuteAsync(null);

        StringAssert.Contains(_dialogs.Errors.Single().Message, "disk full");
        Assert.AreEqual(1, _dialogs.Infos.Count, "the inject itself is reported as done");
        Assert.AreEqual(1, vm.Step);
    }

    [TestMethod]
    public async Task InjectCommand_Fails_RemembersNothing()
    {
        var vm = Ready();
        _factory.Service.Throws = new InvalidOperationException("boom");

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _history.Added.Count);
    }

    [TestMethod]
    public void Load_Record_FillsEveryStepAndLandsOnReview()
    {
        var vm = Create();
        var record = Record();

        vm.Load(record);

        Assert.AreEqual(InjectViewModel.Steps.Count, vm.Step);
        Assert.AreEqual(SourceConsole.Wii, vm.SelectedConsole);
        Assert.AreEqual(record.BaseTitleId, vm.SelectedBase!.Base.TitleId);
        Assert.AreEqual(@"C:\wii.iso", vm.RomPath);
        Assert.AreEqual("Wii Game", vm.Name);
        Assert.AreEqual("Wii", vm.ShortName);
        Assert.AreEqual("WXYZ", vm.ProductId);
        Assert.IsTrue(vm.GamePad);
        Assert.AreEqual(OutputFormat.Loadiine, vm.Format);
        Assert.AreEqual(@"C:\h\icon.png", vm.Icon.Path);
        Assert.AreEqual(@"C:\h\tv.png", vm.BootTv.Path);
        Assert.AreEqual(@"C:\h\drc.png", vm.BootDrc.Path);
        Assert.AreEqual(@"C:\h\logo.png", vm.BootLogo.Path);
        Assert.AreEqual(@"C:\h\boot.wav", vm.BootSound.Path);
        var wii = (WiiOptionsViewModel)vm.CurrentOptions;
        Assert.AreEqual(WiiVideoMode.Pal60, wii.VideoMode);
        Assert.IsTrue(wii.LrPatch);
        Assert.AreEqual(Region.Europe, wii.TargetRegion.Region);
        Assert.IsTrue(vm.CanInject);
    }

    [TestMethod]
    public async Task Load_ThenInject_KeepsTheRecordedIdsSoTheTitleIsReplaced()
    {
        var vm = Create();
        var record = Record();
        vm.Load(record);

        await vm.InjectCommand.ExecuteAsync(null);

        var game = _factory.Service.Received!.Game;
        Assert.AreEqual(record.Identity.TitleId, game.TitleId);
        Assert.AreEqual(record.Identity.GroupId, game.GroupId);
        Assert.AreEqual(record.Identity.ProductCode, game.ProductCode);
        Assert.AreEqual(record.Identity, _history.Added.Single().Record.Identity);
    }

    [TestMethod]
    public async Task Load_ThenStartOver_MintsFreshIds()
    {
        var vm = Create();
        var record = Record();
        vm.Load(record);
        vm.StartOver();
        vm.RomPath = @"C:\game.nes";
        vm.Name = "Other";

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreNotEqual(record.Identity.TitleId, _factory.Service.Received!.Game.TitleId);
    }

    [TestMethod]
    public void Load_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Create().Load(null!));
    }

    private InjectionRecord Record() =>
        new("abc", DateTimeOffset.Now, SourceConsole.Wii, new TitleId(TitleType.Game, 0x1000 + (uint)SourceConsole.Wii), @"C:\wii.iso", "Wii Game",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "WXYZ")))
        {
            Artwork = new Artwork { Icon = @"C:\h\icon.png", BootTv = @"C:\h\tv.png", BootDrc = @"C:\h\drc.png", BootLogo = @"C:\h\logo.png" },
            BootSoundPath = @"C:\h\boot.wav",
            Format = OutputFormat.Loadiine,
            GamePad = true,
            Options = new WiiOptions { VideoMode = WiiVideoMode.Pal60, LrPatch = true, TargetRegion = Region.Europe },
            ProductId = "WXYZ",
            ShortName = "Wii",
        };

    [TestMethod]
    public async Task StartOverCommand_BlankWizard_ClearsWithoutAsking()
    {
        var vm = Create();
        vm.Step = 3;

        await vm.StartOverCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _dialogs.Confirms.Count);
        Assert.AreEqual(1, vm.Step);
    }

    [TestMethod]
    public async Task StartOverCommand_Confirmed_ClearsEverything()
    {
        var vm = Ready(SourceConsole.Snes, @"C:\game.sfc");
        vm.Step = 4;
        vm.Icon.Path = @"C:\icon.png";
        _dialogs.ConfirmResult = true;

        await vm.StartOverCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _dialogs.Confirms.Count);
        Assert.AreEqual(1, vm.Step);
        Assert.AreEqual(SourceConsole.Nes, vm.SelectedConsole);
        Assert.IsNull(vm.RomPath);
        Assert.IsNull(vm.Name);
        Assert.IsNull(vm.Icon.Path);
    }

    [TestMethod]
    public async Task StartOverCommand_Declined_KeepsEverything()
    {
        var vm = Ready(SourceConsole.Snes, @"C:\game.sfc");
        vm.Step = 3;
        _dialogs.ConfirmResult = false;

        await vm.StartOverCommand.ExecuteAsync(null);

        Assert.AreEqual(3, vm.Step);
        Assert.AreEqual(@"C:\game.sfc", vm.RomPath);
        Assert.AreEqual("Game", vm.Name);
    }

    [TestMethod]
    public void ArrowNavigation_Wizard_TurnsSteps()
    {
        IArrowNavigation vm = Create();

        Assert.AreEqual("Next step", vm.NextHint);
        Assert.AreEqual("Previous step", vm.PreviousHint);
        Assert.IsTrue(vm.NextCommand.CanExecute(null));
        Assert.IsFalse(vm.PreviousCommand.CanExecute(null));

        vm.NextCommand.Execute(null);

        Assert.AreEqual(2, ((InjectViewModel)vm).Step);
        Assert.IsTrue(vm.PreviousCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task InjectCommand_Succeeds_ClearsEveryFieldAndReturnsToTheFirstStep()
    {
        var vm = Ready(SourceConsole.Snes, @"C:\game.sfc");
        vm.Step = 5;
        vm.ShortName = "Short";
        vm.ProductId = "ABCD";
        vm.GamePad = true;
        vm.Format = OutputFormat.Loadiine;
        vm.Icon.Path = @"C:\icon.png";
        vm.BootTv.Path = @"C:\tv.png";
        vm.BootDrc.Path = @"C:\drc.png";
        vm.BootLogo.Path = @"C:\logo.png";
        vm.BootSound.Path = @"C:\boot.wav";
        vm.ArtworkBuilder.Tv.SourcePath = @"C:\shot.png";
        vm.ArtworkBuilder.Tv.ReleaseYear = "1994";
        vm.ArtworkBuilder.Logo.LogoText = "Logo";

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(1, vm.Step);
        Assert.AreEqual(SourceConsole.Nes, vm.SelectedConsole);
        Assert.IsNull(vm.RomPath);
        Assert.IsNull(vm.Name);
        Assert.IsNull(vm.ShortName);
        Assert.IsNull(vm.ProductId);
        Assert.IsFalse(vm.GamePad);
        Assert.AreEqual(OutputFormat.Wup, vm.Format);
        Assert.IsNull(vm.Icon.Path);
        Assert.IsNull(vm.BootTv.Path);
        Assert.IsNull(vm.BootDrc.Path);
        Assert.IsNull(vm.BootLogo.Path);
        Assert.IsNull(vm.BootSound.Path);
        Assert.IsNull(vm.ArtworkBuilder.Tv.SourcePath);
        Assert.IsNull(vm.ArtworkBuilder.Tv.ReleaseYear);
        Assert.IsNull(vm.ArtworkBuilder.Tv.NameLine1);
        Assert.IsNull(vm.ArtworkBuilder.Logo.LogoText);
        Assert.IsTrue(vm.ArtworkBuilder.Icon.Overlays.Count > 0, "overlays for the starting console are back");
        Assert.AreEqual(0, vm.Log.Count);
        Assert.IsNull(vm.Status);
        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public async Task InjectCommand_Fails_KeepsEverythingSoItCanBeRetried()
    {
        var vm = Ready(SourceConsole.Snes, @"C:\game.sfc");
        vm.Step = 5;
        vm.Icon.Path = @"C:\icon.png";
        _factory.Service.Throws = new InvalidOperationException("boom");

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(5, vm.Step);
        Assert.AreEqual(SourceConsole.Snes, vm.SelectedConsole);
        Assert.AreEqual(@"C:\game.sfc", vm.RomPath);
        Assert.AreEqual(@"C:\icon.png", vm.Icon.Path);
        Assert.AreEqual("Failed", vm.Status);
    }

    private ArtworkBuilderViewModel Builder() => new(new FakeArtworkComposer(), _dialogs, () => _settings.WorkPath);

    private InjectViewModel Ready(SourceConsole console = SourceConsole.Nes, string rom = @"C:\game.nes")
    {
        var vm = Create();
        vm.SelectedConsole = console;
        vm.RomPath = rom;
        vm.Name = "Game";
        return vm;
    }
}
