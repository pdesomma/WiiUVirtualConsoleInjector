using PD.WiiU.VirtualConsole;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectionServiceFactoryTests
{
    private FakeKeyStore _keys = null!;
    private InjectSettingsService _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _keys = new FakeKeyStore { CommonKey = CommonKey.Parse("00112233445566778899AABBCCDDEEFF") };
        _settings = new InjectSettingsService();
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionServiceFactory(null!, _keys));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionServiceFactory(_settings, null!));
    }

    [TestMethod]
    public void Create_WiiUCommonKeyOnly_HasGameCubeButNotWii()
    {
        var service = (InjectionService)new InjectionServiceFactory(_settings, _keys).Create();

        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.GameCube);
        CollectionAssert.DoesNotContain(service.SupportedConsoles.ToArray(), SourceConsole.Wii);
        foreach (var console in new[] { SourceConsole.Nes, SourceConsole.Snes, SourceConsole.N64, SourceConsole.Gba, SourceConsole.Nds, SourceConsole.Tg16, SourceConsole.Msx })
            CollectionAssert.Contains(service.SupportedConsoles.ToArray(), console);
    }

    [TestMethod]
    public void Create_WiiCommonKeyToo_HasWii()
    {
        _keys.WiiCommonKey = new WiiSharp.CommonKey(new byte[16]);

        var service = (InjectionService)new InjectionServiceFactory(_settings, _keys).Create();

        CollectionAssert.Contains(service.SupportedConsoles.ToArray(), SourceConsole.Wii);
    }

    [TestMethod]
    public void Create_NoWiiUCommonKey_ThrowsInvalidOperationException()
    {
        _keys.CommonKey = null;

        Assert.ThrowsExactly<InvalidOperationException>(() => new InjectionServiceFactory(_settings, _keys).Create());
    }

    [TestMethod]
    public void MissingKeys_WiiNeedsTheWiiKeyGameCubeDoesNot()
    {
        var factory = new InjectionServiceFactory(_settings, _keys);

        CollectionAssert.AreEqual(new[] { InjectionServiceFactory.WiiCommonKeyName }, factory.MissingKeys(SourceConsole.Wii).ToArray());
        Assert.AreEqual(0, factory.MissingKeys(SourceConsole.GameCube).Count);
        Assert.AreEqual(0, factory.MissingKeys(SourceConsole.Nes).Count);
    }

    [TestMethod]
    public void MissingKeys_NoWiiUCommonKey_NamesIt()
    {
        _keys.CommonKey = null;

        var missing = new InjectionServiceFactory(_settings, _keys).MissingKeys(SourceConsole.Wii);

        CollectionAssert.AreEqual(new[] { InjectionServiceFactory.WiiUCommonKeyName, InjectionServiceFactory.WiiCommonKeyName }, missing.ToArray());
    }
}
