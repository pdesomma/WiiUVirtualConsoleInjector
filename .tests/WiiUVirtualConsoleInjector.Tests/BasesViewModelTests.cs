using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class BasesViewModelTests
{
    private const string KeyHexText = "0f1e2d3c4b5a69788796a5b4c3d2e1f0";
    private const string TitleKeyHexText = "00112233445566778899aabbccddeeff";
    private static readonly TitleId NesId = TitleId.Parse("0005000010153100");
    private static readonly TitleId SnesId = TitleId.Parse("0005000010107C00");

    private FakeBaseService _bases = null!;
    private FakeDialogService _dialogs = null!;
    private FakeInjectionServiceFactory _injections = null!;
    private FakeKeyStore _keys = null!;

    [TestInitialize]
    public void Initialize()
    {
        _keys = new FakeKeyStore();
        _bases = new FakeBaseService { Keys = _keys };
        _bases.Titles.Add(new BaseTitle(NesId, "Dr. Mario", Region.UnitedStates, SourceConsole.Nes));
        _bases.Titles.Add(new BaseTitle(SnesId, "Super Metroid", Region.Europe, SourceConsole.Snes));
        _dialogs = new FakeDialogService();
        _injections = new FakeInjectionServiceFactory();
    }

    [TestMethod]
    public void Constructor_Defaults_ListsFirstConsoleAndKeyStatuses()
    {
        var vm = Create();

        Assert.AreEqual("Bases & Keys", vm.Title);
        Assert.AreEqual(SourceConsole.Nes, vm.SelectedConsole);
        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual("Dr. Mario", vm.Bases[0].Name);
        Assert.AreEqual("UnitedStates", vm.Bases[0].Region);
        Assert.AreEqual("0005000010153100", vm.Bases[0].TitleId);
        Assert.AreEqual("Needs common key", vm.Bases[0].StatusText);
        Assert.AreEqual(KeyEntryViewModel.NotSetText, vm.WiiUCommonKeyEntry.Status);
        Assert.AreEqual(KeyEntryViewModel.NotSetText, vm.WiiCommonKeyEntry.Status);
        Assert.AreEqual(KeyEntryViewModel.NotSetText, vm.AncastKeyEntry.Status);
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BasesViewModel(null!, _keys, _injections, _dialogs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BasesViewModel(_bases, null!, _injections, _dialogs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BasesViewModel(_bases, _keys, null!, _dialogs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BasesViewModel(_bases, _keys, _injections, null!));
    }

    [TestMethod]
    public async Task WiiUCommonKey_SaveValidHex_StoresKeyAndRefreshesRows()
    {
        var vm = Create();
        vm.WiiUCommonKeyEntry.Text = $"  {KeyHexText.ToUpperInvariant()}\n";

        await vm.WiiUCommonKeyEntry.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual(KeyHexText, _keys.CommonKey!.ToString());
        Assert.AreEqual(KeyEntryViewModel.SetText, vm.WiiUCommonKeyEntry.Status);
        Assert.IsTrue(vm.WiiUCommonKeyEntry.IsSet);
        Assert.AreEqual(KeyHexText, vm.WiiUCommonKeyEntry.Text);
        Assert.AreEqual(BaseStatus.NeedsTitleKey, vm.Bases[0].Status);
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task WiiUCommonKey_SaveInvalidHex_ShowsErrorAndLeavesStore()
    {
        var vm = Create();
        vm.WiiUCommonKeyEntry.Text = "not a key";

        await vm.WiiUCommonKeyEntry.SaveCommand.ExecuteAsync(null);

        Assert.IsNull(_keys.CommonKey);
        Assert.AreEqual(1, _dialogs.Errors.Count);
        Assert.AreEqual("Wii U common key", _dialogs.Errors[0].Title);
        Assert.AreEqual(KeyEntryViewModel.NotSetText, vm.WiiUCommonKeyEntry.Status);
        Assert.AreEqual("not a key", vm.WiiUCommonKeyEntry.Text);
    }

    [TestMethod]
    public void WiiUCommonKey_TypeValidHex_StoresWithoutSave()
    {
        var vm = Create();

        vm.WiiUCommonKeyEntry.Text = KeyHexText.ToUpperInvariant();

        Assert.AreEqual(KeyHexText, _keys.CommonKey!.ToString());
        Assert.IsTrue(vm.WiiUCommonKeyEntry.IsValid);
        Assert.IsTrue(vm.WiiUCommonKeyEntry.IsSet);
        Assert.AreEqual(KeyHexText, vm.WiiUCommonKeyEntry.Text);
        Assert.AreEqual(BaseStatus.NeedsTitleKey, vm.Bases[0].Status);
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public void WiiUCommonKey_TypeInvalidHex_LeavesStoreAndIsNotValid()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        var vm = Create();
        var changed = new List<string>();
        vm.WiiUCommonKeyEntry.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.WiiUCommonKeyEntry.Text = KeyHexText + "0";

        Assert.AreEqual(KeyHexText, _keys.CommonKey!.ToString());
        Assert.IsFalse(vm.WiiUCommonKeyEntry.IsValid);
        Assert.IsTrue(vm.WiiUCommonKeyEntry.IsSet);
        CollectionAssert.Contains(changed, nameof(KeyEntryViewModel.IsValid));
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public void WiiUCommonKey_TypeEmpty_ClearsStoredKey()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        var vm = Create();
        Assert.IsTrue(vm.WiiUCommonKeyEntry.IsValid);

        vm.WiiUCommonKeyEntry.Text = " ";

        Assert.IsNull(_keys.CommonKey);
        Assert.IsFalse(vm.WiiUCommonKeyEntry.IsSet);
        Assert.IsFalse(vm.WiiUCommonKeyEntry.IsValid);
        Assert.AreEqual(string.Empty, vm.WiiUCommonKeyEntry.Text);
        Assert.AreEqual(BaseStatus.NeedsCommonKey, vm.Bases[0].Status);
    }

    [TestMethod]
    public void WiiUCommonKey_Refresh_DoesNotStoreAgain()
    {
        var saves = 0;
        var entry = new KeyEntryViewModel("Key", () => KeyHexText, _ => saves++, () => { }, _dialogs, () => { });

        entry.Refresh();

        Assert.AreEqual(0, saves);
        Assert.IsTrue(entry.IsValid);
    }

    [TestMethod]
    public void WiiUCommonKey_Clear_RemovesKeyAndRefreshesRows()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        var vm = Create();
        Assert.AreEqual(BaseStatus.NeedsTitleKey, vm.Bases[0].Status);

        vm.WiiUCommonKeyEntry.ClearCommand.Execute(null);

        Assert.IsNull(_keys.CommonKey);
        Assert.AreEqual(KeyEntryViewModel.NotSetText, vm.WiiUCommonKeyEntry.Status);
        Assert.AreEqual(string.Empty, vm.WiiUCommonKeyEntry.Text);
        Assert.AreEqual(BaseStatus.NeedsCommonKey, vm.Bases[0].Status);
    }

    [TestMethod]
    public async Task WiiCommonKey_SaveValidHex_StoresKey()
    {
        var vm = Create();
        vm.WiiCommonKeyEntry.Text = KeyHexText;

        await vm.WiiCommonKeyEntry.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual(KeyHexText, KeyHex.Format(_keys.WiiCommonKey!.Value.ToArray()));
        Assert.AreEqual(KeyEntryViewModel.SetText, vm.WiiCommonKeyEntry.Status);
    }

    [TestMethod]
    public async Task AncastKey_SaveValidHex_StoresKey()
    {
        var vm = Create();
        vm.AncastKeyEntry.Text = KeyHexText;

        await vm.AncastKeyEntry.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual(AncastKey.Parse(KeyHexText), _keys.AncastKey);
        Assert.AreEqual(KeyEntryViewModel.SetText, vm.AncastKeyEntry.Status);
    }

    [TestMethod]
    public async Task ImportOtp_ValidFile_StoresCommonKey()
    {
        var path = WriteOtp(0x100);
        try
        {
            _dialogs.FileToPick = path;
            var vm = Create();

            await vm.ImportOtpCommand.ExecuteAsync(null);

            Assert.IsNotNull(_keys.CommonKey);
            Assert.AreEqual(KeyHexText, _keys.CommonKey!.ToString());
            Assert.AreEqual(KeyEntryViewModel.SetText, vm.WiiUCommonKeyEntry.Status);
            Assert.AreEqual(BaseStatus.NeedsTitleKey, vm.Bases[0].Status);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ImportOtp_Cancelled_DoesNothing()
    {
        var vm = Create();

        await vm.ImportOtpCommand.ExecuteAsync(null);

        Assert.IsNull(_keys.CommonKey);
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task ImportOtp_ShortFile_ShowsError()
    {
        var path = WriteOtp(0x20);
        try
        {
            _dialogs.FileToPick = path;
            var vm = Create();

            await vm.ImportOtpCommand.ExecuteAsync(null);

            Assert.IsNull(_keys.CommonKey);
            Assert.AreEqual(1, _dialogs.Errors.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ActivateAsync_KeysChangedElsewhere_RefreshesEntriesAndRows()
    {
        var vm = Create();
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        _keys.SetTitleKey(NesId, EncryptedTitleKey.Parse(TitleKeyHexText));

        await vm.ActivateAsync();

        Assert.AreEqual(KeyEntryViewModel.SetText, vm.WiiUCommonKeyEntry.Status);
        Assert.AreEqual(BaseStatus.Downloadable, vm.Bases[0].Status);
        Assert.AreEqual(TitleKeyHexText, vm.Bases[0].TitleKey);
        Assert.IsTrue(vm.Bases[0].CanDownload);
    }

    [TestMethod]
    public void SelectedConsole_Changed_ReloadsRows()
    {
        var vm = Create();

        vm.SelectedConsole = SourceConsole.Snes;

        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual("Super Metroid", vm.Bases[0].Name);
    }

    [TestMethod]
    public async Task SaveTitleKey_ValidHex_StoresAndRefreshes()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        var vm = Create();
        var row = vm.Bases[0];
        row.TitleKey = $" {TitleKeyHexText.ToUpperInvariant()} ";

        await row.SaveTitleKeyCommand.ExecuteAsync(null);

        Assert.AreEqual(EncryptedTitleKey.Parse(TitleKeyHexText), _keys.GetTitleKey(NesId));
        Assert.AreEqual(TitleKeyHexText, row.TitleKey);
        Assert.AreEqual(BaseStatus.Downloadable, row.Status);
        Assert.AreEqual("Downloadable", row.StatusText);
    }

    [TestMethod]
    public async Task SaveTitleKey_Empty_ClearsKey()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        _keys.SetTitleKey(NesId, EncryptedTitleKey.Parse(TitleKeyHexText));
        var vm = Create();
        var row = vm.Bases[0];
        row.TitleKey = "  ";

        await row.SaveTitleKeyCommand.ExecuteAsync(null);

        Assert.IsNull(_keys.GetTitleKey(NesId));
        Assert.AreEqual(BaseStatus.NeedsTitleKey, row.Status);
    }

    [TestMethod]
    public void TitleKey_TypeValidHex_StoresWithoutSave()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        var vm = Create();
        var row = vm.Bases[0];
        Assert.IsTrue(row.NeedsKey);
        Assert.IsFalse(row.IsTitleKeyValid);

        row.TitleKey = TitleKeyHexText.ToUpperInvariant();

        Assert.AreEqual(EncryptedTitleKey.Parse(TitleKeyHexText), _keys.GetTitleKey(NesId));
        Assert.AreEqual(TitleKeyHexText, row.TitleKey);
        Assert.IsTrue(row.IsTitleKeyValid);
        Assert.IsFalse(row.NeedsKey);
        Assert.IsFalse(row.IsPresent);
        Assert.AreEqual(BaseStatus.Downloadable, row.Status);
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public void TitleKey_TypeInvalidHex_LeavesStoreAndIsNotValid()
    {
        _keys.SetTitleKey(NesId, EncryptedTitleKey.Parse(TitleKeyHexText));
        var vm = Create();
        var row = vm.Bases[0];
        var changed = new List<string>();
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        row.TitleKey = "zz";

        Assert.AreEqual(EncryptedTitleKey.Parse(TitleKeyHexText), _keys.GetTitleKey(NesId));
        Assert.IsFalse(row.IsTitleKeyValid);
        Assert.AreEqual("zz", row.TitleKey);
        CollectionAssert.Contains(changed, nameof(BaseRowViewModel.IsTitleKeyValid));
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public void TitleKey_TypeEmpty_ClearsStoredKey()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        _keys.SetTitleKey(NesId, EncryptedTitleKey.Parse(TitleKeyHexText));
        var vm = Create();
        var row = vm.Bases[0];
        Assert.IsTrue(row.IsTitleKeyValid);

        row.TitleKey = "";

        Assert.IsNull(_keys.GetTitleKey(NesId));
        Assert.AreEqual(BaseStatus.NeedsTitleKey, row.Status);
        Assert.IsTrue(row.NeedsKey);
    }

    [TestMethod]
    public void TitleKey_TypeEmptyWithNothingStored_DoesNotWrite()
    {
        var vm = Create();
        var row = vm.Bases[0];

        row.TitleKey = " ";

        Assert.AreEqual(0, _keys.TitleKeyWrites);
    }

    [TestMethod]
    public void Status_Present_SetsPillFlags()
    {
        var row = PresentRow();

        Assert.IsTrue(row.IsPresent);
        Assert.IsFalse(row.NeedsKey);
    }

    [TestMethod]
    public async Task SaveTitleKey_InvalidHex_ShowsErrorAndLeavesStore()
    {
        var vm = Create();
        var row = vm.Bases[0];
        row.TitleKey = "zz";

        await row.SaveTitleKeyCommand.ExecuteAsync(null);

        Assert.IsNull(_keys.GetTitleKey(NesId));
        Assert.AreEqual(1, _dialogs.Errors.Count);
        Assert.AreEqual("zz", row.TitleKey);
    }

    [TestMethod]
    public async Task Download_Succeeds_MarksPresent()
    {
        var row = DownloadableRow();
        _bases.Download = (b, _, _) =>
        {
            _bases.Present.Add(b.TitleId);
            return Task.FromResult(new TitleDirectory(Path.GetTempPath()));
        };

        await row.DownloadCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _bases.Downloads.Count);
        Assert.AreEqual("Downloaded", row.ProgressText);
        Assert.AreEqual(BaseStatus.Present, row.Status);
        Assert.IsTrue(row.CanInspect);
        Assert.IsFalse(row.CanDownload);
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task Download_InvalidData_SaysKeysAreWrong()
    {
        var row = DownloadableRow();
        _bases.Download = (_, _, _) => throw new InvalidDataException("bad hash");

        await row.DownloadCommand.ExecuteAsync(null);

        Assert.AreEqual("Failed", row.ProgressText);
        Assert.AreEqual(1, _dialogs.Errors.Count);
        StringAssert.Contains(_dialogs.Errors[0].Message, "keys do not unlock");
        StringAssert.Contains(_dialogs.Errors[0].Message, "bad hash");
        Assert.AreEqual(BaseStatus.Downloadable, row.Status);
    }

    [TestMethod]
    public async Task Download_OtherError_ShowsMessage()
    {
        var row = DownloadableRow();
        _bases.Download = (_, _, _) => throw new HttpRequestException("offline");

        await row.DownloadCommand.ExecuteAsync(null);

        Assert.AreEqual("Failed", row.ProgressText);
        Assert.AreEqual(1, _dialogs.Errors.Count);
        Assert.AreEqual("offline", _dialogs.Errors[0].Message);
    }

    [TestMethod]
    public async Task Download_Cancelled_SaysCancelledWithoutError()
    {
        var row = DownloadableRow();
        _bases.Download = async (_, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new TitleDirectory(Path.GetTempPath());
        };

        var download = row.DownloadCommand.ExecuteAsync(null);
        Assert.IsTrue(row.DownloadCommand.IsRunning);
        row.DownloadCancelCommand.Execute(null);
        await download;

        Assert.AreEqual("Cancelled", row.ProgressText);
        Assert.AreEqual(0, _dialogs.Errors.Count);
        Assert.AreEqual(BaseStatus.Downloadable, row.Status);
    }

    [TestMethod]
    public void Download_NotDownloadable_CannotExecute()
    {
        var vm = Create();

        Assert.IsFalse(vm.Bases[0].DownloadCommand.CanExecute(null));
        Assert.IsFalse(vm.Bases[0].InspectCommand.CanExecute(null));
    }

    [TestMethod]
    public void Report_Downloading_FormatsFileCountAndPercent()
    {
        var row = DownloadableRow();

        row.Report(new BaseDownloadProgress(BaseDownloadPhase.Downloading, "00000003.app", 4, 12, 35, 100));
        Assert.AreEqual("Downloading 00000003.app (4/12) 35%", row.ProgressText);

        row.Report(new BaseDownloadProgress(BaseDownloadPhase.Downloading, "title.tmd", 1, 0, 0, null));
        Assert.AreEqual("Downloading title.tmd", row.ProgressText);

        row.Report(new BaseDownloadProgress(BaseDownloadPhase.Unpacking, "00000003", 3, 12, 0, null));
        Assert.AreEqual("Unpacking content 00000003", row.ProgressText);

        Assert.ThrowsExactly<ArgumentNullException>(() => row.Report(null!));
    }

    [TestMethod]
    public async Task Inspect_NoIssues_SaysUsable()
    {
        var row = PresentRow();

        await row.InspectCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _injections.Service.Inspected.Count);
        Assert.AreEqual(1, _dialogs.Infos.Count);
        Assert.AreEqual("Base is usable", _dialogs.Infos[0].Message);
    }

    [TestMethod]
    public async Task Inspect_Issues_ListsThem()
    {
        var row = PresentRow();
        _injections.Service.Issues.Add(new BaseIssue("meta/meta.xml", "missing"));
        _injections.Service.Issues.Add(new BaseIssue("content/rom.zip", "empty"));

        await row.InspectCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _dialogs.Infos.Count);
        Assert.AreEqual("meta/meta.xml: missing" + Environment.NewLine + "content/rom.zip: empty", _dialogs.Infos[0].Message);
    }

    [TestMethod]
    public async Task Inspect_FactoryThrows_ShowsError()
    {
        var row = PresentRow();
        _injections.CreateError = new InvalidOperationException("no common key");

        await row.InspectCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _dialogs.Infos.Count);
        Assert.AreEqual(1, _dialogs.Errors.Count);
        Assert.AreEqual("no common key", _dialogs.Errors[0].Message);
    }

    [TestMethod]
    public async Task AddCustomBase_Valid_AddsRowThatSurvivesConsoleSwitch()
    {
        var vm = Create();
        vm.CustomTitleId = " 0005000010199900 ";
        vm.CustomName = " Homebrew NES ";
        vm.CustomRegion = Region.Japan;

        await vm.AddCustomBaseCommand.ExecuteAsync(null);

        Assert.AreEqual(2, vm.Bases.Count);
        var row = vm.Bases[1];
        Assert.IsTrue(row.IsCustom);
        Assert.AreEqual("Homebrew NES", row.Name);
        Assert.AreEqual("Japan", row.Region);
        Assert.AreEqual("0005000010199900", row.TitleId);
        Assert.AreEqual(SourceConsole.Nes, row.Base.Console);
        Assert.AreEqual(string.Empty, vm.CustomTitleId);
        Assert.AreEqual(string.Empty, vm.CustomName);

        vm.SelectedConsole = SourceConsole.Snes;
        Assert.AreEqual(1, vm.Bases.Count);
        vm.SelectedConsole = SourceConsole.Nes;
        Assert.AreEqual(2, vm.Bases.Count);
        Assert.IsTrue(vm.Bases[1].IsCustom);
    }

    [TestMethod]
    public async Task AddCustomBase_InvalidTitleId_ShowsError()
    {
        var vm = Create();
        vm.CustomTitleId = "1234";
        vm.CustomName = "Thing";

        await vm.AddCustomBaseCommand.ExecuteAsync(null);

        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual(1, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task AddCustomBase_BlankName_ShowsError()
    {
        var vm = Create();
        vm.CustomTitleId = "0005000010199900";
        vm.CustomName = " ";

        await vm.AddCustomBaseCommand.ExecuteAsync(null);

        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual(1, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task AddCustomBase_DuplicateTitleId_ShowsError()
    {
        var vm = Create();
        vm.CustomTitleId = "0005000010153100";
        vm.CustomName = "Again";

        await vm.AddCustomBaseCommand.ExecuteAsync(null);

        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual(1, _dialogs.Errors.Count);
    }

    private BasesViewModel Create() => new(_bases, _keys, _injections, _dialogs);

    private BaseRowViewModel DownloadableRow()
    {
        _keys.CommonKey = CommonKey.Parse(KeyHexText);
        _keys.SetTitleKey(NesId, EncryptedTitleKey.Parse(TitleKeyHexText));
        var row = Create().Bases[0];
        Assert.AreEqual(BaseStatus.Downloadable, row.Status);
        return row;
    }

    private BaseRowViewModel PresentRow()
    {
        _bases.Present.Add(NesId);
        var row = Create().Bases[0];
        Assert.AreEqual(BaseStatus.Present, row.Status);
        return row;
    }

    private static string WriteOtp(int length)
    {
        var bytes = new byte[length];
        if (length >= 0xF0)
            KeyHex.Parse(KeyHexText, 16).CopyTo(bytes, 0xE0);
        var path = Path.Combine(Path.GetTempPath(), $"otp-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
