namespace PD.WiiU.VirtualConsole.Gba;

/// <summary>
/// Zeroes the two save-size checks that stop Pokémon GBA games saving on the Virtual Console.
/// </summary>
public static class PokemonPatch
{
    private static readonly byte[] Pattern = { 0xD0, 0x88, 0x8D, 0x83, 0x42 };

    /// <summary>
    /// Patches the first two matches in place; leaves the ROM alone when fewer are found.
    /// </summary>
    /// <param name="rom">GBA ROM bytes.</param>
    /// <returns>True when both sites were patched.</returns>
    public static bool Apply(byte[] rom)
    {
        if (rom is null)
            throw new ArgumentNullException(nameof(rom));

        var sites = new List<int>();
        for (var i = 0; i + Pattern.Length + 4 <= rom.Length && sites.Count < 2; i++)
        {
            var match = true;
            for (var j = 0; j < Pattern.Length && match; j++)
                match = rom[i + j] == Pattern[j];
            if (match)
                sites.Add(i);
        }
        if (sites.Count < 2)
            return false;

        foreach (var site in sites)
        {
            var at = site + Pattern.Length;
            var count = rom[at + 3] == 0x24 ? 3 : 4;
            Array.Clear(rom, at, count);
        }
        return true;
    }
}
