using WiiUVirtualConsoleInjector.Behaviors;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class AutoScrollTests
{
    [TestMethod]
    public void ShouldFollow_ViewSittingAtTheBottom_True()
    {
        Assert.IsTrue(AutoScroll.ShouldFollow(offset: 340, extent: 500, viewport: 160));
        Assert.IsTrue(AutoScroll.ShouldFollow(offset: 340 - AutoScroll.Slack, extent: 500, viewport: 160), "within slack");
    }

    [TestMethod]
    public void ShouldFollow_UserScrolledUp_False()
    {
        Assert.IsFalse(AutoScroll.ShouldFollow(offset: 0, extent: 500, viewport: 160));
        Assert.IsFalse(AutoScroll.ShouldFollow(offset: 200, extent: 500, viewport: 160));
    }

    [TestMethod]
    public void ShouldFollow_ContentShorterThanTheView_True()
    {
        Assert.IsTrue(AutoScroll.ShouldFollow(offset: 0, extent: 40, viewport: 160), "nothing to scroll yet");
    }
}
