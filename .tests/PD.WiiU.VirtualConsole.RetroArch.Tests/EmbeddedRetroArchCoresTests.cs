namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class EmbeddedRetroArchCoresTests
{
    private const int GenesisPlusGxLength = 6729787;

    private string _root = null!;

    private static RetroArchCore GenesisPlusGx => EmbeddedRetroArchCores.All.Single(core => core.Id == "genesis_plus_gx" && core.Console == SourceConsole.Genesis);

    [TestInitialize]
    public void Initialize() => _root = TestPaths.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void All_Always_HasThreeGenesisCoresInDeclarationOrder()
    {
        var genesis = EmbeddedRetroArchCores.All.Where(core => core.Console == SourceConsole.Genesis).ToArray();

        Assert.AreEqual(3, genesis.Length);
        CollectionAssert.AreEqual(new[] { "genesis_plus_gx", "genesis_plus_gx_wide", "picodrive" }, genesis.Select(core => core.Id).ToArray());
    }

    [TestMethod]
    public void All_Always_RecommendsNothing()
    {
        Assert.IsFalse(EmbeddedRetroArchCores.All.Any(core => core.IsRecommended));
    }

    [TestMethod]
    public void All_EveryConsole_HasACoreAndASystem()
    {
        var cores = new EmbeddedRetroArchCores();
        foreach (var console in EmbeddedRetroArchCores.All.Select(core => core.Console).Distinct())
        {
            Assert.IsTrue(cores.Available(console).Count >= 1, console.ToString());
            Assert.IsNotNull(cores.System(console), console.ToString());
        }
        foreach (var system in EmbeddedRetroArchCores.Systems)
            Assert.IsTrue(cores.Available(system.Console).Count >= 1, system.Console.ToString());
    }

    [TestMethod]
    public void Available_Arcade_ListsTenCoresFbneoFirst()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(
            new[] { "fbneo", "mame2003_plus", "mame2010", "mame2000", "mame2003_midway", "fbalpha2012", "fbalpha2012_cps1", "fbalpha2012_cps2", "fbalpha2012_cps3", "fbalpha2012_neogeo" },
            cores.Available(SourceConsole.Arcade).Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { ".zip", ".7z" }, cores.System(SourceConsole.Arcade)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Arcade)!.BiosFiles.Count);
    }

    [TestMethod]
    public void Available_NeoGeo_IsFbneoOnlyWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var neoGeo = cores.Available(SourceConsole.NeoGeo);
        Assert.AreEqual(1, neoGeo.Count);
        Assert.AreEqual("fbneo", neoGeo[0].Id);
        Assert.AreEqual("fbneo_libretro.rpx", neoGeo[0].RpxFileName);
        StringAssert.Contains(neoGeo[0].Description, "neogeo.zip");
        CollectionAssert.AreEqual(new[] { ".zip", ".7z" }, cores.System(SourceConsole.NeoGeo)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.NeoGeo)!.BiosFiles.Count);
    }

    [TestMethod]
    public void Available_MasterSystemAndGameGear_ShareGenesisPlusGxWithGearsystem()
    {
        var cores = new EmbeddedRetroArchCores();
        foreach (var console in new[] { SourceConsole.MasterSystem, SourceConsole.GameGear })
            CollectionAssert.AreEqual(new[] { "genesis_plus_gx", "gearsystem" }, cores.Available(console).Select(core => core.Id).ToArray(), console.ToString());
        CollectionAssert.AreEqual(new[] { "picodrive" }, cores.Available(SourceConsole.Sega32X).Select(core => core.Id).ToArray());
    }

    [TestMethod]
    public void System_AtariLynx_NeedsTheBootRom()
    {
        var cores = new EmbeddedRetroArchCores();

        var lynx = cores.System(SourceConsole.AtariLynx)!.BiosFiles.Single();
        Assert.AreEqual("lynxboot.img", lynx.Label);
        CollectionAssert.AreEqual(new[] { "lynxboot.img" }, lynx.Names.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Atari2600)!.BiosFiles.Count);
        Assert.AreEqual(0, cores.System(SourceConsole.Atari7800)!.BiosFiles.Count);
        CollectionAssert.AreEqual(new[] { "stella2023" }, cores.Available(SourceConsole.Atari2600).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "prosystem" }, cores.Available(SourceConsole.Atari7800).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "handy" }, cores.Available(SourceConsole.AtariLynx).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "mednafen_vb" }, cores.Available(SourceConsole.VirtualBoy).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { ".vb", ".vboy" }, cores.System(SourceConsole.VirtualBoy)!.Extensions.ToArray());
    }

    [TestMethod]
    public void System_PlayStation_TakesDiscImagesAndWantsAnyOneBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var playStation = cores.Available(SourceConsole.PlayStation);
        Assert.AreEqual(1, playStation.Count);
        Assert.AreEqual("pcsx_rearmed", playStation[0].Id);
        Assert.AreEqual("PCSX-ReARMed", playStation[0].Name);
        Assert.AreEqual("pcsx_rearmed_libretro.rpx", playStation[0].RpxFileName);
        StringAssert.Contains(playStation[0].Description, "BIOS");
        var system = cores.System(SourceConsole.PlayStation)!;
        CollectionAssert.AreEqual(new[] { ".cue", ".chd", ".pbp", ".m3u", ".iso", ".img" }, system.Extensions.ToArray());
        var bios = system.BiosFiles.Single();
        Assert.AreEqual("PlayStation BIOS", bios.Label);
        CollectionAssert.AreEqual(new[] { "scph5501.bin", "scph5500.bin", "scph5502.bin", "scph1001.bin", "psxonpsp660.bin" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void Available_NeoGeoPocket_ListsBeetleNeoPopThenRace()
    {
        var cores = new EmbeddedRetroArchCores();

        var pocket = cores.Available(SourceConsole.NeoGeoPocket);
        CollectionAssert.AreEqual(new[] { "mednafen_ngp", "race" }, pocket.Select(core => core.Id).ToArray());
        Assert.AreEqual("Beetle NeoPop", pocket[0].Name);
        Assert.AreEqual("RACE", pocket[1].Name);
        Assert.AreEqual("mednafen_ngp_libretro.rpx", pocket[0].RpxFileName);
        var system = cores.System(SourceConsole.NeoGeoPocket)!;
        CollectionAssert.AreEqual(new[] { ".ngp", ".ngc", ".ngpc", ".npc" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count);
    }

    [TestMethod]
    public void Available_OneFileHandhelds_HaveOneCoreEachAndNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(new[] { "pokemini" }, cores.Available(SourceConsole.PokemonMini).Select(core => core.Id).ToArray());
        Assert.AreEqual("PokeMini", cores.Available(SourceConsole.PokemonMini)[0].Name);
        CollectionAssert.AreEqual(new[] { ".min" }, cores.System(SourceConsole.PokemonMini)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.PokemonMini)!.BiosFiles.Count);

        CollectionAssert.AreEqual(new[] { "mednafen_wswan" }, cores.Available(SourceConsole.WonderSwan).Select(core => core.Id).ToArray());
        Assert.AreEqual("Beetle WonderSwan", cores.Available(SourceConsole.WonderSwan)[0].Name);
        CollectionAssert.AreEqual(new[] { ".ws", ".wsc", ".pc2", ".pcv2" }, cores.System(SourceConsole.WonderSwan)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.WonderSwan)!.BiosFiles.Count);

        CollectionAssert.AreEqual(new[] { "potator" }, cores.Available(SourceConsole.Supervision).Select(core => core.Id).ToArray());
        Assert.AreEqual("Potator", cores.Available(SourceConsole.Supervision)[0].Name);
        CollectionAssert.AreEqual(new[] { ".bin", ".sv" }, cores.System(SourceConsole.Supervision)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Supervision)!.BiosFiles.Count);

        CollectionAssert.AreEqual(new[] { "gw" }, cores.Available(SourceConsole.GameAndWatch).Select(core => core.Id).ToArray());
        Assert.AreEqual("GW", cores.Available(SourceConsole.GameAndWatch)[0].Name);
        CollectionAssert.AreEqual(new[] { ".mgw" }, cores.System(SourceConsole.GameAndWatch)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.GameAndWatch)!.BiosFiles.Count);

        CollectionAssert.AreEqual(new[] { "vecx" }, cores.Available(SourceConsole.Vectrex).Select(core => core.Id).ToArray());
        Assert.AreEqual("vecx", cores.Available(SourceConsole.Vectrex)[0].Name);
        CollectionAssert.AreEqual(new[] { ".bin", ".vec" }, cores.System(SourceConsole.Vectrex)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Vectrex)!.BiosFiles.Count);
    }

    [TestMethod]
    public void System_ColecoVision_NeedsTheColecoRom()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(new[] { "gearcoleco" }, cores.Available(SourceConsole.ColecoVision).Select(core => core.Id).ToArray());
        Assert.AreEqual("Gearcoleco", cores.Available(SourceConsole.ColecoVision)[0].Name);
        var system = cores.System(SourceConsole.ColecoVision)!;
        CollectionAssert.AreEqual(new[] { ".col", ".cv", ".bin", ".rom" }, system.Extensions.ToArray());
        var bios = system.BiosFiles.Single();
        Assert.AreEqual("colecovision.rom", bios.Label);
        CollectionAssert.AreEqual(new[] { "colecovision.rom" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void System_Intellivision_NeedsExecAndGrom()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(new[] { "freeintv" }, cores.Available(SourceConsole.Intellivision).Select(core => core.Id).ToArray());
        Assert.AreEqual("FreeIntv", cores.Available(SourceConsole.Intellivision)[0].Name);
        var system = cores.System(SourceConsole.Intellivision)!;
        CollectionAssert.AreEqual(new[] { ".int", ".bin", ".rom" }, system.Extensions.ToArray());
        CollectionAssert.AreEqual(new[] { "exec.bin", "grom.bin" }, system.BiosFiles.Select(bios => bios.Label).ToArray());
        foreach (var bios in system.BiosFiles)
            CollectionAssert.AreEqual(new[] { bios.Label }, bios.Names.ToArray(), bios.Label);
    }

    [TestMethod]
    public void System_Odyssey2_NeedsOnlyTheG7000Rom()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(new[] { "o2em" }, cores.Available(SourceConsole.Odyssey2).Select(core => core.Id).ToArray());
        Assert.AreEqual("O2EM", cores.Available(SourceConsole.Odyssey2)[0].Name);
        var system = cores.System(SourceConsole.Odyssey2)!;
        CollectionAssert.AreEqual(new[] { ".bin" }, system.Extensions.ToArray());
        var bios = system.BiosFiles.Single();
        Assert.AreEqual("o2rom.bin", bios.Label);
        CollectionAssert.AreEqual(new[] { "o2rom.bin" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void System_SegaCd_TakesDiscImagesAndWantsOneRegionBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var segaCd = cores.Available(SourceConsole.SegaCd);
        CollectionAssert.AreEqual(new[] { "genesis_plus_gx", "picodrive" }, segaCd.Select(core => core.Id).ToArray());
        Assert.AreEqual("Genesis Plus GX", segaCd[0].Name);
        Assert.AreEqual("genesis_plus_gx_libretro.rpx", segaCd[0].RpxFileName);
        StringAssert.Contains(segaCd[0].Description, "BIOS");
        var system = cores.System(SourceConsole.SegaCd)!;
        CollectionAssert.AreEqual(new[] { ".cue", ".chd", ".iso", ".m3u" }, system.Extensions.ToArray());
        var bios = system.BiosFiles.Single();
        Assert.AreEqual("Sega CD BIOS", bios.Label);
        CollectionAssert.AreEqual(new[] { "bios_CD_U.bin", "bios_CD_E.bin", "bios_CD_J.bin" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void System_NeoGeoCd_TakesCueOrChdAndWantsZoomRomPlusOneSystemRom()
    {
        var cores = new EmbeddedRetroArchCores();

        var neoGeoCd = cores.Available(SourceConsole.NeoGeoCd);
        Assert.AreEqual(1, neoGeoCd.Count);
        Assert.AreEqual("neocd", neoGeoCd[0].Id);
        Assert.AreEqual("NeoCD", neoGeoCd[0].Name);
        Assert.AreEqual("neocd_libretro.rpx", neoGeoCd[0].RpxFileName);
        StringAssert.Contains(neoGeoCd[0].Description, "retroarch/system/neocd");
        var system = cores.System(SourceConsole.NeoGeoCd)!;
        CollectionAssert.AreEqual(new[] { ".cue", ".chd" }, system.Extensions.ToArray());
        Assert.AreEqual(2, system.BiosFiles.Count);
        Assert.AreEqual("Neo Geo CD zoom ROM", system.BiosFiles[0].Label);
        CollectionAssert.AreEqual(new[] { "neocd/000-lo.lo", "neocd/ng-lo.rom" }, system.BiosFiles[0].Names.ToArray());
        Assert.AreEqual("Neo Geo CD BIOS", system.BiosFiles[1].Label);
        CollectionAssert.AreEqual(
            new[] { "neocd/neocd_z.rom", "neocd/neocd_f.rom", "neocd/neocd_sf.rom", "neocd/front-sp1.bin", "neocd/neocd_t.rom", "neocd/neocd_st.rom", "neocd/top-sp1.bin", "neocd/neocd_sz.rom", "neocd/neocd.bin", "neocd/uni-bioscd.rom" },
            system.BiosFiles[1].Names.ToArray());
        Assert.IsTrue(system.BiosFiles.SelectMany(bios => bios.Names).All(name => name.StartsWith("neocd/", StringComparison.Ordinal)), "every name sits under the core's own folder");
    }

    [TestMethod]
    public void System_Dos_TakesZipsExecutablesAndDiscImagesWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var dos = cores.Available(SourceConsole.Dos);
        Assert.AreEqual(1, dos.Count);
        Assert.AreEqual("dosbox_pure", dos[0].Id);
        Assert.AreEqual("DOSBox-pure", dos[0].Name);
        Assert.AreEqual("dosbox_pure_libretro.rpx", dos[0].RpxFileName);
        var system = cores.System(SourceConsole.Dos)!;
        CollectionAssert.AreEqual(
            new[] { ".zip", ".dosz", ".exe", ".com", ".bat", ".iso", ".cue", ".ins", ".img", ".ima", ".vhd", ".jrc", ".tc", ".m3u", ".m3u8", ".conf" },
            system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count);
        Assert.IsTrue(system.Accepts(@"C:\games\Prince of Persia.zip"));
    }

    [TestMethod]
    public void System_Commodore_TakesTheSameViceImagesOnBothMachinesWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();
        var expected = new[] { ".d64", ".d71", ".d80", ".d81", ".d82", ".g64", ".g41", ".x64", ".t64", ".tap", ".prg", ".p00", ".crt", ".bin", ".d6z", ".d7z", ".d8z", ".g6z", ".g4z", ".x6z", ".cmd", ".m3u", ".vfl", ".vsf", ".nib", ".nbz", ".d2m", ".d4m" };

        CollectionAssert.AreEqual(new[] { "vice_x64" }, cores.Available(SourceConsole.Commodore64).Select(core => core.Id).ToArray());
        Assert.AreEqual("VICE x64", cores.Available(SourceConsole.Commodore64)[0].Name);
        CollectionAssert.AreEqual(expected, cores.System(SourceConsole.Commodore64)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Commodore64)!.BiosFiles.Count);

        CollectionAssert.AreEqual(new[] { "vice_x128" }, cores.Available(SourceConsole.Commodore128).Select(core => core.Id).ToArray());
        Assert.AreEqual("VICE x128", cores.Available(SourceConsole.Commodore128)[0].Name);
        CollectionAssert.AreEqual(expected, cores.System(SourceConsole.Commodore128)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Commodore128)!.BiosFiles.Count);
        Assert.IsFalse(cores.System(SourceConsole.Commodore64)!.Accepts("game.zip"), "archives are not disk images");
    }

    [TestMethod]
    public void Available_AmstradCpc_ListsCaprice32ThenCrocoDsAndTakesBothTheirFormats()
    {
        var cores = new EmbeddedRetroArchCores();

        var cpc = cores.Available(SourceConsole.AmstradCpc);
        CollectionAssert.AreEqual(new[] { "cap32", "crocods" }, cpc.Select(core => core.Id).ToArray());
        Assert.AreEqual("Caprice32", cpc[0].Name);
        Assert.AreEqual("CrocoDS", cpc[1].Name);
        Assert.AreEqual("crocods_libretro.rpx", cpc[1].RpxFileName);
        var system = cores.System(SourceConsole.AmstradCpc)!;
        CollectionAssert.AreEqual(new[] { ".dsk", ".sna", ".tap", ".cdt", ".voc", ".cpr", ".m3u", ".kcr" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count);
    }

    [TestMethod]
    public void System_ZxSpectrum_TakesTapesSnapshotsAndDisksWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(new[] { "fuse" }, cores.Available(SourceConsole.ZxSpectrum).Select(core => core.Id).ToArray());
        Assert.AreEqual("Fuse", cores.Available(SourceConsole.ZxSpectrum)[0].Name);
        var system = cores.System(SourceConsole.ZxSpectrum)!;
        CollectionAssert.AreEqual(new[] { ".tzx", ".tap", ".z80", ".rzx", ".scl", ".trd", ".dsk" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count);
    }

    [TestMethod]
    public void System_AtariSt_TakesDiskImagesAndNeedsTos()
    {
        var cores = new EmbeddedRetroArchCores();

        var atariSt = cores.Available(SourceConsole.AtariSt);
        Assert.AreEqual(1, atariSt.Count);
        Assert.AreEqual("hatari", atariSt[0].Id);
        Assert.AreEqual("Hatari", atariSt[0].Name);
        StringAssert.Contains(atariSt[0].Description, "tos.img");
        var system = cores.System(SourceConsole.AtariSt)!;
        CollectionAssert.AreEqual(new[] { ".st", ".msa", ".stx", ".dim", ".ipf", ".vhd", ".gem", ".ide", ".m3u" }, system.Extensions.ToArray());
        var bios = system.BiosFiles.Single();
        Assert.AreEqual("tos.img", bios.Label);
        CollectionAssert.AreEqual(new[] { "tos.img" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void System_KnownAndUnknown()
    {
        var cores = new EmbeddedRetroArchCores();
        CollectionAssert.AreEqual(new[] { ".sms" }, cores.System(SourceConsole.MasterSystem)!.Extensions.ToArray());
        CollectionAssert.AreEqual(new[] { ".gg" }, cores.System(SourceConsole.GameGear)!.Extensions.ToArray());
        CollectionAssert.AreEqual(new[] { ".32x", ".bin" }, cores.System(SourceConsole.Sega32X)!.Extensions.ToArray());
        Assert.IsTrue(cores.System(SourceConsole.Genesis)!.Accepts("game.MD"));
        foreach (var console in new[] { SourceConsole.N64, SourceConsole.Nds, SourceConsole.GameCube, SourceConsole.Wii })
        {
            Assert.IsNull(cores.System(console), console.ToString());
            Assert.AreEqual(0, cores.Available(console).Count, console.ToString());
        }
    }

    [TestMethod]
    public void Available_Nes_ListsFourCoresFceummFirstWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var nes = cores.Available(SourceConsole.Nes);
        CollectionAssert.AreEqual(new[] { "fceumm", "nestopia", "quicknes", "fixnes" }, nes.Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "FCEUmm", "Nestopia", "QuickNES", "fixNES" }, nes.Select(core => core.Name).ToArray());
        Assert.AreEqual("fceumm_libretro.rpx", nes[0].RpxFileName);
        var system = cores.System(SourceConsole.Nes)!;
        CollectionAssert.AreEqual(new[] { ".nes", ".fds", ".unf", ".unif", ".qd", ".nsf" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count, "disksys.rom is optional in every core");
        Assert.IsTrue(system.Accepts(@"C:\roms\Metroid.NES"));
    }

    [TestMethod]
    public void Available_Snes_ListsSixCoresSnes9xFirstWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var snes = cores.Available(SourceConsole.Snes);
        CollectionAssert.AreEqual(new[] { "snes9x", "snes9x2010", "snes9x2005_plus", "snes9x2005", "snes9x2002", "chimerasnes" }, snes.Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Snes9x", "Snes9x 2010", "Snes9x 2005 Plus", "Snes9x 2005", "Snes9x 2002", "ChimeraSNES" }, snes.Select(core => core.Name).ToArray());
        var system = cores.System(SourceConsole.Snes)!;
        CollectionAssert.AreEqual(new[] { ".smc", ".sfc", ".swc", ".fig", ".bs", ".st", ".gd3", ".gd7", ".dx2", ".bsx" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count);
    }

    [TestMethod]
    public void Available_GameBoy_ListsThreeCoresGambatteFirstWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var gameBoy = cores.Available(SourceConsole.GameBoy);
        CollectionAssert.AreEqual(new[] { "gambatte", "gearboy", "fixgb" }, gameBoy.Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Gambatte", "Gearboy", "fixGB" }, gameBoy.Select(core => core.Name).ToArray());
        StringAssert.Contains(gameBoy[0].Description, "Color");
        var system = cores.System(SourceConsole.GameBoy)!;
        CollectionAssert.AreEqual(new[] { ".gb", ".gbc", ".dmg", ".cgb", ".sgb", ".gbs" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count, "the boot ROMs are optional");
        Assert.IsFalse(system.Accepts("game.gba"), "GBA carts belong to the GBA system");
    }

    [TestMethod]
    public void Available_Gba_ListsFourCoresMgbaFirstAndTakesOnlyGbaCarts()
    {
        var cores = new EmbeddedRetroArchCores();

        var gba = cores.Available(SourceConsole.Gba);
        CollectionAssert.AreEqual(new[] { "mgba", "vbam", "vba_next", "gpsp" }, gba.Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "mGBA", "VBA-M", "VBA Next", "gpSP" }, gba.Select(core => core.Name).ToArray());
        var system = cores.System(SourceConsole.Gba)!;
        CollectionAssert.AreEqual(new[] { ".gba", ".bin" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count, "gba_bios.bin is optional in mGBA");
        Assert.IsFalse(system.Accepts("game.gb"), "Game Boy carts belong to the Game Boy system");
    }

    [TestMethod]
    public void Available_Tg16_ListsThreeBeetleCoresAndTakesHuCardsAndDiscsWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var tg16 = cores.Available(SourceConsole.Tg16);
        CollectionAssert.AreEqual(new[] { "mednafen_pce", "mednafen_pce_fast", "mednafen_supergrafx" }, tg16.Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Beetle PCE", "Beetle PCE Fast", "Beetle SuperGrafx" }, tg16.Select(core => core.Name).ToArray());
        StringAssert.Contains(tg16[1].Description, "no SuperGrafx");
        var system = cores.System(SourceConsole.Tg16)!;
        CollectionAssert.AreEqual(new[] { ".pce", ".sgx", ".cue", ".ccd", ".chd", ".toc", ".m3u" }, system.Extensions.ToArray());
        Assert.AreEqual(0, system.BiosFiles.Count, "the CD system cards are optional in every core");
    }

    [TestMethod]
    public void Available_Msx_ListsBlueMsxThenFmsxAndWantsTheFmsxSystemRoms()
    {
        var cores = new EmbeddedRetroArchCores();

        var msx = cores.Available(SourceConsole.Msx);
        CollectionAssert.AreEqual(new[] { "bluemsx", "fmsx" }, msx.Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "blueMSX", "fMSX" }, msx.Select(core => core.Name).ToArray());
        StringAssert.Contains(msx[0].Description, "Machines");
        StringAssert.Contains(msx[0].Description, "Databases");
        var system = cores.System(SourceConsole.Msx)!;
        CollectionAssert.AreEqual(new[] { ".rom", ".mx1", ".mx2", ".ri", ".col", ".sg", ".sc", ".sf", ".dsk", ".fdi", ".cas", ".m3u" }, system.Extensions.ToArray());
        CollectionAssert.AreEqual(new[] { "MSX.ROM", "MSX2.ROM", "MSX2EXT.ROM", "MSX2P.ROM", "MSX2PEXT.ROM" }, system.BiosFiles.Select(bios => bios.Label).ToArray());
        foreach (var bios in system.BiosFiles)
            CollectionAssert.AreEqual(new[] { bios.Label }, bios.Names.ToArray(), bios.Label);
    }

    [TestMethod]
    public void All_VirtualConsoleConsolesWithCores_HaveASystemAndViceVersa()
    {
        var cores = new EmbeddedRetroArchCores();
        var withBases = new[] { SourceConsole.Nes, SourceConsole.Snes, SourceConsole.GameBoy, SourceConsole.Gba, SourceConsole.Tg16, SourceConsole.Msx };

        foreach (var console in withBases)
        {
            Assert.IsTrue(cores.Available(console).Count >= 2, console.ToString());
            Assert.IsNotNull(cores.System(console), console.ToString());
            Assert.AreEqual(console, cores.System(console)!.Console);
        }
        CollectionAssert.AreEquivalent(withBases, EmbeddedRetroArchCores.Systems.Select(s => s.Console).Intersect(withBases).ToArray());
        foreach (var console in new[] { SourceConsole.N64, SourceConsole.Nds, SourceConsole.GameCube, SourceConsole.Wii })
            Assert.IsFalse(EmbeddedRetroArchCores.Systems.Any(s => s.Console == console), console + " has no core yet");
    }

    [TestMethod]
    public void All_EveryCore_IsEmbeddedAtTheCatalogedSize()
    {
        var sizes = new Dictionary<string, long>
        {
            ["genesis_plus_gx"] = GenesisPlusGxLength,
            ["genesis_plus_gx_wide"] = 6717132,
            ["picodrive"] = 6156010,
            ["gearsystem"] = 5841357,
            ["stella2023"] = 7344333,
            ["prosystem"] = 5471817,
            ["handy"] = 5516495,
            ["mednafen_vb"] = 5494527,
            ["fbneo"] = 29751521,
            ["mame2003_plus"] = 18012178,
            ["mame2010"] = 24833358,
            ["mame2000"] = 10261117,
            ["mame2003_midway"] = 6345351,
            ["fbalpha2012"] = 12335794,
            ["fbalpha2012_cps1"] = 6054951,
            ["fbalpha2012_cps2"] = 5944447,
            ["fbalpha2012_cps3"] = 5506159,
            ["fbalpha2012_neogeo"] = 6099616,
            ["pcsx_rearmed"] = 6231138,
            ["pokemini"] = 5545354,
            ["mednafen_ngp"] = 5582405,
            ["race"] = 5551384,
            ["mednafen_wswan"] = 5593151,
            ["potator"] = 5437345,
            ["gw"] = 5643936,
            ["gearcoleco"] = 5823933,
            ["freeintv"] = 5451978,
            ["o2em"] = 5520131,
            ["vecx"] = 5467604,
            ["neocd"] = 6419656,
            ["dosbox_pure"] = 7414602,
            ["vice_x64"] = 7577989,
            ["vice_x128"] = 7903418,
            ["cap32"] = 5806774,
            ["crocods"] = 5694943,
            ["fuse"] = 6533579,
            ["hatari"] = 7079134,
            ["fceumm"] = 6090653,
            ["nestopia"] = 6675352,
            ["quicknes"] = 5596135,
            ["fixnes"] = 5607437,
            ["snes9x"] = 6644460,
            ["snes9x2010"] = 6154753,
            ["snes9x2005_plus"] = 5754543,
            ["snes9x2005"] = 5791443,
            ["snes9x2002"] = 5809676,
            ["chimerasnes"] = 5715847,
            ["gambatte"] = 5782792,
            ["gearboy"] = 5823995,
            ["fixgb"] = 5472504,
            ["mgba"] = 5974622,
            ["vbam"] = 5969366,
            ["vba_next"] = 5722814,
            ["gpsp"] = 5655979,
            ["mednafen_pce"] = 5964384,
            ["mednafen_pce_fast"] = 5893942,
            ["mednafen_supergrafx"] = 5881503,
            ["bluemsx"] = 6258329,
            ["fmsx"] = 5576407,
        };

        foreach (var core in EmbeddedRetroArchCores.All)
        {
            Assert.IsTrue(sizes.ContainsKey(core.Id), core.Id);
            using var stream = typeof(EmbeddedRetroArchCores).Assembly.GetManifestResourceStream(EmbeddedRetroArchCores.ResourceName(core));
            Assert.IsNotNull(stream, core.Id);
            Assert.AreEqual(sizes[core.Id], stream!.Length, core.Id);
        }
    }

    [TestMethod]
    public void All_EveryCore_HasItsResourceCompiledIn()
    {
        var names = typeof(EmbeddedRetroArchCores).Assembly.GetManifestResourceNames();

        foreach (var core in EmbeddedRetroArchCores.All)
            CollectionAssert.Contains(names, EmbeddedRetroArchCores.ResourceName(core), core.Id);
    }

    [TestMethod]
    public void Available_Genesis_KeepsDeclarationOrder()
    {
        var cores = new EmbeddedRetroArchCores().Available(SourceConsole.Genesis);

        Assert.AreEqual(3, cores.Count);
        Assert.AreEqual("genesis_plus_gx", cores[0].Id);
        Assert.AreEqual("picodrive", cores[2].Id);
        Assert.IsFalse(cores.Any(core => core.IsRecommended));
    }

    [TestMethod]
    public void ResourceName_GenesisPlusGx_IsCoresPath()
    {
        Assert.AreEqual("cores/genesis_plus_gx_libretro.rpx", EmbeddedRetroArchCores.ResourceName(GenesisPlusGx));
    }

    [TestMethod]
    public void ResourceName_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => EmbeddedRetroArchCores.ResourceName(null!));
    }

    [TestMethod]
    public async Task StageAsync_BlankDestination_ThrowsArgumentException()
    {
        var cores = new EmbeddedRetroArchCores();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => cores.StageAsync(GenesisPlusGx, " "));
    }

    [TestMethod]
    public async Task StageAsync_CoreNotBundled_ThrowsArgumentException()
    {
        var cores = new EmbeddedRetroArchCores();
        var bsnes = new RetroArchCore("bsnes", "bsnes", SourceConsole.Snes, "x");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => cores.StageAsync(bsnes, _root));
    }

    [TestMethod]
    public async Task StageAsync_GenesisPlusGx_WritesCoreAndTemplate()
    {
        var title = await new EmbeddedRetroArchCores().StageAsync(GenesisPlusGx, _root);

        Assert.IsTrue(title.Exists, "code, content and meta");
        var rpx = Path.Combine(title.Code, "genesis_plus_gx_libretro.rpx");
        Assert.IsTrue(File.Exists(rpx));
        Assert.AreEqual(GenesisPlusGxLength, new FileInfo(rpx).Length);
        using (var stream = File.OpenRead(rpx))
        {
            var magic = new byte[4];
            Assert.AreEqual(4, stream.Read(magic, 0, 4));
            CollectionAssert.AreEqual(new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' }, magic);
        }
        Assert.IsTrue(File.Exists(title.AppXmlPath));
        Assert.IsTrue(File.Exists(Path.Combine(title.Code, CosXml.FileName)));
        Assert.IsTrue(File.Exists(title.MetaXmlPath));
        foreach (var file in RetroArchTemplate.MetaFiles)
            Assert.IsTrue(File.Exists(Path.Combine(title.Root, file.Replace('/', Path.DirectorySeparatorChar))), file);
        Assert.IsTrue(new BaseInspection(title).Layout().Passed);
        Assert.AreEqual(0, new RetroArchRomInjector(SourceConsole.Genesis).Inspect(title).Count);
    }

    [TestMethod]
    public async Task StageAsync_NullCore_ThrowsArgumentNullException()
    {
        var cores = new EmbeddedRetroArchCores();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => cores.StageAsync(null!, _root));
    }

    [TestMethod]
    public async Task StageAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cores = new EmbeddedRetroArchCores();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => cores.StageAsync(GenesisPlusGx, _root, cancellation.Token));
    }
}
