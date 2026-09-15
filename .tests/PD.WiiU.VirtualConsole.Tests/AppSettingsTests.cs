namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class AppSettingsTests
{
    [TestMethod]
    public void Defaults_NewInstance_HasNoPathsAndNoSuppressedWarnings()
    {
        var settings = new AppSettings();

        Assert.IsNull(settings.BasePath);
        Assert.IsNull(settings.OutputPath);
        Assert.IsNull(settings.WorkPath);
        Assert.AreEqual(0, settings.SuppressedWarnings.Count);
        Assert.IsFalse(settings.IsSuppressed(InjectionWarning.NdsDsiEnhanced));
    }

    [TestMethod]
    public void Suppress_ThenRestore_TogglesWithoutMutatingTheOriginal()
    {
        var original = new AppSettings { BasePath = @"C:\bases" };

        var suppressed = original.Suppress(InjectionWarning.SnesCoProcessor).Suppress(InjectionWarning.SnesCoProcessor);
        var restored = suppressed.Restore(InjectionWarning.SnesCoProcessor);

        Assert.IsTrue(suppressed.IsSuppressed(InjectionWarning.SnesCoProcessor));
        Assert.AreEqual(1, suppressed.SuppressedWarnings.Count, "suppressing twice keeps one entry");
        Assert.AreEqual(@"C:\bases", suppressed.BasePath);
        Assert.IsFalse(restored.IsSuppressed(InjectionWarning.SnesCoProcessor));
        Assert.IsFalse(original.IsSuppressed(InjectionWarning.SnesCoProcessor));
    }

    [TestMethod]
    public void SuppressedWarnings_Init_DeduplicatesAndRejectsNull()
    {
        var settings = new AppSettings { SuppressedWarnings = new[] { InjectionWarning.GameCubeGcz, InjectionWarning.GameCubeGcz, InjectionWarning.NdsDsiEnhanced } };

        CollectionAssert.AreEquivalent(new[] { InjectionWarning.GameCubeGcz, InjectionWarning.NdsDsiEnhanced }, settings.SuppressedWarnings.ToArray());
        Assert.ThrowsExactly<ArgumentNullException>(() => new AppSettings { SuppressedWarnings = null! });
    }
}
