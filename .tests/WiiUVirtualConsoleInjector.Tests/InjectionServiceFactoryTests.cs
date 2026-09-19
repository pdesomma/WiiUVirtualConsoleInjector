using PD.WiiU.VirtualConsole;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectionServiceFactoryTests
{
    private FakeRetroArchCores _cores = null!;
    private FakeKeyStore _keys = null!;
    private InjectSettingsService _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _cores = new FakeRetroArchCores();
        _keys = new FakeKeyStore { CommonKey = CommonKey.Parse("00112233445566778899AABBCCDDEEFF") };
        _settings = new InjectSettingsService();
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionServiceFactory(null!, _keys, _cores));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionServiceFactory(_settings, null!, _cores));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionServiceFactory(_settings, _keys, null!));
    }

    [TestMethod]
    public void Create_WiiUCommonKeyOnly_HasGameCubeAndWii()
    {
        var service = (InjectionService)new InjectionServiceFactory(_settings, _keys, _cores).Create();

        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.GameCube);
        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.Wii, "NKit images, homebrew and channels need no Wii key");
        foreach (var console in new[] { SourceConsole.Nes, SourceConsole.Snes, SourceConsole.N64, SourceConsole.Gba, SourceConsole.GameBoy, SourceConsole.Nds, SourceConsole.Tg16, SourceConsole.Msx })
            CollectionAssert.Contains(service.SupportedConsoles.ToArray(), console);
    }

    [TestMethod]
    public void Create_Always_HasGenesisThroughRetroArch()
    {
        var service = (InjectionService)new InjectionServiceFactory(_settings, _keys, _cores).Create();

        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.Genesis);
    }

    [TestMethod]
    public void Create_GameBoyOnTheGbaInjector_SupportedBesideNes()
    {
        var service = (InjectionService)new InjectionServiceFactory(_settings, _keys, _cores).Create();

        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.GameBoy);
        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.Nes, "a base injector and a core injector for the same console coexist");
    }

    [TestMethod]
    public void Create_WiiCommonKeyToo_HasWii()
    {
        _keys.WiiCommonKey = new WiiSharp.CommonKey(new byte[16]);

        var service = (InjectionService)new InjectionServiceFactory(_settings, _keys, _cores).Create();

        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.Wii);
    }

    [TestMethod]
    public void Create_NoWiiUCommonKey_ThrowsInvalidOperationException()
    {
        _keys.CommonKey = null;

        Assert.ThrowsExactly<InvalidOperationException>(() => new InjectionServiceFactory(_settings, _keys, _cores).Create());
    }

    [TestMethod]
    public void MissingKeys_WiiNeedsTheWiiKeyOnlyForAnEncryptedDisc()
    {
        var factory = new InjectionServiceFactory(_settings, _keys, _cores);
        var root = Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var encrypted = Path.Combine(root, "game.iso");
            File.WriteAllBytes(encrypted, new byte[0x300]);
            var nkit = Path.Combine(root, "game.nkit.iso");
            var block = new byte[0x300];
            System.Text.Encoding.ASCII.GetBytes("NKIT v01").CopyTo(block, 0x200);
            File.WriteAllBytes(nkit, block);

            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Wii).Count, "no ROM yet");
            CollectionAssert.AreEqual(new[] { InjectionServiceFactory.WiiCommonKeyName }, factory.MissingKeys(SourceConsole.Wii, encrypted).ToArray());
            CollectionAssert.AreEqual(new[] { InjectionServiceFactory.WiiCommonKeyName }, factory.MissingKeys(SourceConsole.Wii, Path.Combine(root, "game.wbfs")).ToArray());
            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Wii, nkit).Count, "NKit is plaintext");
            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Wii, Path.Combine(root, "app.dol")).Count);
            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Wii, Path.Combine(root, "channel.wad")).Count);
            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.GameCube, encrypted).Count);
            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Nes).Count);

            _keys.WiiCommonKey = new WiiSharp.CommonKey(new byte[16]);
            Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Wii, encrypted).Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void MissingKeys_NoWiiUCommonKey_NamesIt()
    {
        _keys.CommonKey = null;

        var missing = new InjectionServiceFactory(_settings, _keys, _cores).MissingKeys(SourceConsole.Wii, @"C:\game.wbfs");

        CollectionAssert.AreEqual(new[] { InjectionServiceFactory.WiiUCommonKeyName, InjectionServiceFactory.WiiCommonKeyName }, missing.ToArray());
    }
}
