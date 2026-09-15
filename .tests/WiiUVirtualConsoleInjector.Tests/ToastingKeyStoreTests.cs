using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ToastingKeyStoreTests
{
    private static readonly TitleId Id = TitleId.Parse("0005000010153100");

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ToastingKeyStore(null!, new FakeToastService()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ToastingKeyStore(new FakeKeyStore(), null!));
    }

    [TestMethod]
    public void Setters_EveryWrite_PassesThroughAndToastsSaved()
    {
        var inner = new FakeKeyStore();
        var toasts = new FakeToastService();
        var store = new ToastingKeyStore(inner, toasts);

        store.CommonKey = new CommonKey(new byte[16]);
        store.WiiCommonKey = new WiiSharp.CommonKey(new byte[16]);
        store.AncastKey = new AncastKey(new byte[16]);
        store.SetTitleKey(Id, new EncryptedTitleKey(new byte[16]));
        store.CommonKey = null;

        Assert.IsNull(inner.CommonKey);
        Assert.IsNotNull(inner.WiiCommonKey);
        Assert.IsNotNull(inner.AncastKey);
        Assert.AreEqual(1, inner.TitleKeyWrites);
        Assert.AreEqual(5, toasts.Shown.Count);
        Assert.IsTrue(toasts.Shown.All(m => m == ToastingKeyStore.SavedText));
    }

    [TestMethod]
    public void Getters_Always_ReadThroughWithoutToasting()
    {
        var inner = new FakeKeyStore { CommonKey = new CommonKey(new byte[16]) };
        inner.SetTitleKey(Id, new EncryptedTitleKey(new byte[16]));
        var toasts = new FakeToastService();
        var store = new ToastingKeyStore(inner, toasts);

        Assert.AreEqual(inner.CommonKey, store.CommonKey);
        Assert.IsNull(store.WiiCommonKey);
        Assert.IsNull(store.AncastKey);
        Assert.IsNotNull(store.GetTitleKey(Id));
        Assert.AreEqual(0, toasts.Shown.Count);
    }
}
