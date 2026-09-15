using WiiUVirtualConsoleInjector.Converters;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ToastServiceTests
{
    private FakeUiScheduler _scheduler = null!;
    private ToastService _toasts = null!;

    [TestInitialize]
    public void Setup()
    {
        _scheduler = new FakeUiScheduler();
        _toasts = new ToastService(_scheduler);
    }

    [TestMethod]
    public void Constructor_NullScheduler_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ToastService(null!));
    }

    [TestMethod]
    public void Show_NullTitle_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _toasts.Show(ToastKind.Success, null!));
    }

    [TestMethod]
    public void Show_Toast_PostsToTheUiThreadAndKeepsTheText()
    {
        _toasts.Show(ToastKind.Info, "Base downloaded", "NES Virtual Console Base (USA) is ready to use.");

        var toast = _toasts.Toasts.Single();
        Assert.AreEqual(1, _scheduler.Posted);
        Assert.AreEqual(ToastKind.Info, toast.Kind);
        Assert.AreEqual("Base downloaded", toast.Title);
        Assert.AreEqual("NES Virtual Console Base (USA) is ready to use.", toast.Message);
        Assert.IsTrue(toast.HasMessage);
        Assert.IsFalse(toast.IsLeaving);
        Assert.IsTrue(toast.IsEntering, "starts off-screen");

        _scheduler.RunDue(ToastService.EnterDelay);
        Assert.IsFalse(toast.IsEntering, "slides in");
    }

    [TestMethod]
    public void Show_BlankMessage_HasNoSecondLine()
    {
        _toasts.Show(ToastKind.Success, "Saved", "   ");

        Assert.IsNull(_toasts.Toasts.Single().Message);
        Assert.IsFalse(_toasts.Toasts.Single().HasMessage);
    }

    [TestMethod]
    public void Show_PastTheFifth_DropsTheOldest()
    {
        for (var i = 1; i <= ToastService.MaxVisible + 2; i++)
            _toasts.Show(ToastKind.Success, $"Toast {i}");

        CollectionAssert.AreEqual(
            new[] { "Toast 3", "Toast 4", "Toast 5", "Toast 6" },
            _toasts.Toasts.Select(t => t.Title).ToArray());
    }

    [TestMethod]
    public void Show_AnythingButAnError_LeavesWhenItsLifetimeIsUp()
    {
        _toasts.Show(ToastKind.Success, "Saved");
        _scheduler.RunDue(ToastService.EnterDelay);

        _scheduler.RunDue(ToastService.Lifetime);
        var toast = _toasts.Toasts.Single();
        Assert.IsTrue(toast.IsLeaving, "animates out before it goes");

        _scheduler.RunDue(ToastService.ExitDuration);
        Assert.AreEqual(0, _toasts.Toasts.Count);
        Assert.AreEqual(0, _scheduler.Pending.Count);
    }

    [TestMethod]
    public void Show_AnError_StaysUntilDismissed()
    {
        _toasts.Show(ToastKind.Error, "Injection failed", "The ROM uses a co-processor.");

        _scheduler.RunDue(ToastService.EnterDelay);
        Assert.AreEqual(0, _scheduler.Pending.Count, "no lifetime timer");
        _scheduler.RunDue(ToastService.Lifetime);
        Assert.AreEqual(1, _toasts.Toasts.Count);
    }

    [TestMethod]
    public void DismissCommand_Toast_LeavesThenGoes()
    {
        _toasts.Show(ToastKind.Success, "Saved");
        var toast = _toasts.Toasts.Single();

        toast.DismissCommand.Execute(null);

        Assert.IsTrue(toast.IsLeaving);
        Assert.AreEqual(1, _toasts.Toasts.Count);
        Assert.AreEqual(1, _scheduler.Cancelled, "the lifetime timer is dropped");

        _scheduler.RunDue(ToastService.ExitDuration);
        Assert.AreEqual(0, _toasts.Toasts.Count);
    }

    [TestMethod]
    public void Dismiss_TwiceOrUnknownOrNull_DoesNothingExtra()
    {
        _toasts.Show(ToastKind.Error, "Injection failed");
        var toast = _toasts.Toasts.Single();
        _toasts.Dismiss(toast);
        var pending = _scheduler.Pending.Count;

        _toasts.Dismiss(toast);
        _toasts.Dismiss(new ToastViewModel(ToastKind.Info, "Stranger", null, _toasts));

        Assert.AreEqual(pending, _scheduler.Pending.Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => _toasts.Dismiss(null!));
    }

    [TestMethod]
    public void StripeFor_EveryKind_UsesTheDesignColours()
    {
        Assert.AreSame(ToastConverters.StripeFor(ToastKind.Success), ToastConverters.StripeFor(ToastKind.Info));
        Assert.AreNotSame(ToastConverters.StripeFor(ToastKind.Error), ToastConverters.StripeFor(ToastKind.Warning));
        Assert.AreNotSame(ToastConverters.StripeFor(ToastKind.Error), ToastConverters.StripeFor(ToastKind.Success));
    }

    [TestMethod]
    public void ToastViewModel_NullTitleOrService_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ToastViewModel(ToastKind.Info, null!, null, _toasts));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ToastViewModel(ToastKind.Info, "Title", null, null!));
    }
}
