using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class InjectionServiceTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = TestTitle.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_DuplicateInjectorConsole_ThrowsArgumentException()
    {
        var injectors = new[] { new FakeRomInjector(SourceConsole.N64), new FakeRomInjector(SourceConsole.N64) };

        Assert.ThrowsExactly<ArgumentException>(() => new InjectionService(new FakeBaseStore(), injectors, new FakeImageConverter(), new FakeBootSoundConverter(), new FakeTitlePacker()));
    }

    [TestMethod]
    public void Constructor_NullPort_ThrowsArgumentNullException()
    {
        var injectors = new[] { new FakeRomInjector(SourceConsole.N64) };

        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionService(null!, injectors, new FakeImageConverter(), new FakeBootSoundConverter(), new FakeTitlePacker()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionService(new FakeBaseStore(), null!, new FakeImageConverter(), new FakeBootSoundConverter(), new FakeTitlePacker()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionService(new FakeBaseStore(), injectors, null!, new FakeBootSoundConverter(), new FakeTitlePacker()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionService(new FakeBaseStore(), injectors, new FakeImageConverter(), null!, new FakeTitlePacker()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionService(new FakeBaseStore(), injectors, new FakeImageConverter(), new FakeBootSoundConverter(), null!));
    }

    [TestMethod]
    public async Task InjectAsync_ArtworkForSomeSlots_ConvertsOnlyThoseIntoMeta()
    {
        var images = new FakeImageConverter();
        var packer = new FakeTitlePacker();
        var service = Service(images: images, packer: packer);
        var injection = new Injection(TestTitle.Base(), Rom(), TestTitle.Game())
        {
            Artwork = new Artwork { Icon = @"C:\art\icon.png", BootTv = @"C:\art\tv.jpg" },
        };

        await service.InjectAsync(injection, Work(), Output());

        var title = packer.Calls.Single().Title;
        CollectionAssert.AreEquivalent(
            new[] { (@"C:\art\tv.jpg", ImageSlot.BootTv, Path.Combine(title.Meta, "bootTvTex.tga")), (@"C:\art\icon.png", ImageSlot.Icon, Path.Combine(title.Meta, "iconTex.tga")) },
            images.Calls);
        CollectionAssert.Contains(packer.Calls.Single().MetaFiles, "iconTex.tga");
        CollectionAssert.DoesNotContain(packer.Calls.Single().MetaFiles, "bootDrcTex.tga");
    }

    [TestMethod]
    public async Task InjectAsync_BootSoundSupplied_WritesBtsndIntoMeta()
    {
        var sounds = new FakeBootSoundConverter();
        var packer = new FakeTitlePacker();
        var service = Service(sounds: sounds, packer: packer);
        var injection = new Injection(TestTitle.Base(), Rom(), TestTitle.Game()) { BootSoundPath = @"C:\audio\boot.wav" };

        await service.InjectAsync(injection, Work(), Output());

        var call = sounds.Calls.Single();
        Assert.AreEqual(@"C:\audio\boot.wav", call.Source);
        Assert.AreEqual(Path.Combine(packer.Calls.Single().Title.Meta, BootSound.FileName), call.Destination);
    }

    [TestMethod]
    public async Task InjectAsync_InjectorCancels_PropagatesOperationCanceledUnwrapped()
    {
        var injector = new FakeRomInjector(SourceConsole.N64, (_, _) => throw new OperationCanceledException());
        var service = Service(injector);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => service.InjectAsync(new Injection(TestTitle.Base(), Rom(), TestTitle.Game()), Work(), Output()));
    }

    [TestMethod]
    public async Task InjectAsync_InjectorFails_WrapsInInjectionExceptionAndDeletesWork()
    {
        var injector = new FakeRomInjector(SourceConsole.N64, (_, _) => throw new IOException("disk full"));
        var packer = new FakeTitlePacker();
        var service = Service(injector, packer: packer);

        var error = await Assert.ThrowsExactlyAsync<InjectionException>(() => service.InjectAsync(new Injection(TestTitle.Base(), Rom(), TestTitle.Game()), Work(), Output()));

        Assert.AreEqual(InjectionStep.InjectRom, error.Step);
        Assert.IsInstanceOfType<IOException>(error.InnerException);
        StringAssert.Contains(error.Message, "disk full");
        Assert.AreEqual(0, packer.Calls.Count);
        Assert.AreEqual(0, Directory.GetDirectories(Work()).Length);
    }

    [TestMethod]
    public async Task InjectAsync_MetadataStep_WritesGameIntoBothXmlFiles()
    {
        var packer = new ReadingPacker();
        var service = Service(packer: packer);
        var game = TestTitle.Game();

        await service.InjectAsync(new Injection(TestTitle.Base(), Rom(), game), Work(), Output());

        Assert.AreEqual(game.TitleId, packer.Meta!.TitleId);
        Assert.AreEqual(game.ProductCode, packer.Meta.ProductCode);
        Assert.AreEqual("Test Game", packer.Meta.NameIn(Language.English)!.LongName);
        Assert.AreEqual(game.TitleId, packer.AppTitleId);
    }

    [TestMethod]
    public async Task InjectAsync_NoArtworkOrSound_SkipsThoseConverters()
    {
        var images = new FakeImageConverter();
        var sounds = new FakeBootSoundConverter();
        var steps = new List<InjectionStep>();
        var service = Service(images: images, sounds: sounds);

        await service.InjectAsync(new Injection(TestTitle.Base(), Rom(), TestTitle.Game()), Work(), Output(), new SyncProgress(p => steps.Add(p.Step)));

        Assert.AreEqual(0, images.Calls.Count);
        Assert.AreEqual(0, sounds.Calls.Count);
        CollectionAssert.DoesNotContain(steps, InjectionStep.ConvertBootSound);
    }

    [TestMethod]
    public async Task InjectAsync_NullInjection_ThrowsArgumentNullException()
    {
        var service = Service();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => service.InjectAsync(null!, Work(), Output()));
    }

    [TestMethod]
    public async Task InjectAsync_UnknownConsole_ThrowsNotSupportedException()
    {
        var service = Service(new FakeRomInjector(SourceConsole.Snes));

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => service.InjectAsync(new Injection(TestTitle.Base(), Rom(), TestTitle.Game()), Work(), Output()));
    }

    [TestMethod]
    public async Task InjectAsync_ValidInjection_RunsStepsInOrderAndCleansUp()
    {
        var bases = new FakeBaseStore();
        var injector = new FakeRomInjector(SourceConsole.N64);
        var packer = new FakeTitlePacker();
        var reports = new List<InjectionProgress>();
        var service = new InjectionService(bases, new[] { injector }, new FakeImageConverter(), new FakeBootSoundConverter(), packer);
        var injection = new Injection(TestTitle.Base(), Rom(), TestTitle.Game()) { BootSoundPath = @"C:\audio\boot.wav" };

        var result = await service.InjectAsync(injection, Work(), Output(), new SyncProgress(reports.Add));

        CollectionAssert.AreEqual(
            new[] { InjectionStep.StageBase, InjectionStep.InjectRom, InjectionStep.InjectRom, InjectionStep.WriteMetadata, InjectionStep.ConvertArtwork, InjectionStep.ConvertBootSound, InjectionStep.Pack },
            reports.Select(r => r.Step).ToArray());
        Assert.AreEqual("injecting", reports[2].Message);
        Assert.AreEqual(bases.Destinations.Single(), injector.Titles.Single().Root);
        Assert.AreSame(injector.Titles.Single(), packer.Calls.Single().Title);
        Assert.AreEqual(Output(), packer.Calls.Single().Output);
        Assert.AreEqual(Output(), result.OutputDirectory);
        Assert.AreSame(injection.Game, result.Game);
        Assert.IsTrue(File.Exists(Path.Combine(Output(), "title.tmd")));
        Assert.AreEqual(0, Directory.GetDirectories(Work()).Length);
    }

    [TestMethod]
    public void SupportedConsoles_TwoInjectors_ListsBoth()
    {
        var service = Service(new FakeRomInjector(SourceConsole.Snes), new FakeRomInjector(SourceConsole.Wii));

        CollectionAssert.AreEquivalent(new[] { SourceConsole.Snes, SourceConsole.Wii }, service.SupportedConsoles.ToArray());
    }

    private string Output() => Path.Combine(_root, "out");

    private static Rom Rom() => new(@"C:\roms\game.z64", SourceConsole.N64);

    private static InjectionService Service(params FakeRomInjector[] injectors) =>
        Service(injectors.Length == 0 ? null : injectors, null, null, null);

    private static InjectionService Service(FakeRomInjector? injector = null, FakeImageConverter? images = null, FakeBootSoundConverter? sounds = null, ITitlePacker? packer = null) =>
        Service(injector is null ? null : new[] { injector }, images, sounds, packer);

    private static InjectionService Service(FakeRomInjector[]? injectors, FakeImageConverter? images, FakeBootSoundConverter? sounds, ITitlePacker? packer) =>
        new(new FakeBaseStore(), injectors ?? new[] { new FakeRomInjector(SourceConsole.N64) }, images ?? new FakeImageConverter(), sounds ?? new FakeBootSoundConverter(), packer ?? new FakeTitlePacker());

    private string Work()
    {
        var work = Path.Combine(_root, "work");
        Directory.CreateDirectory(work);
        return work;
    }

    private sealed class ReadingPacker : ITitlePacker
    {
        public TitleId? AppTitleId { get; private set; }
        public Game? Meta { get; private set; }

        public Task PackAsync(TitleDirectory title, string outputDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            Meta = MetaXml.Load(title.MetaXmlPath).Read();
            AppTitleId = AppXml.Load(title.AppXmlPath).ReadTitleId();
            return Task.CompletedTask;
        }
    }

    private sealed class SyncProgress : IProgress<InjectionProgress>
    {
        private readonly Action<InjectionProgress> _report;

        public SyncProgress(Action<InjectionProgress> report)
        {
            _report = report;
        }

        public void Report(InjectionProgress value) => _report(value);
    }
}
