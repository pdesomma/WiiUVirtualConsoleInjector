using System.Globalization;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Builds the identity of an injected title: a fresh title ID, group ID and product code around a name.
/// </summary>
public static class GameFactory
{
    /// <summary>
    /// Smallest sixteen-bit half a generated ID uses; keeps clear of real titles.
    /// </summary>
    public const int MinimumIdHalf = 0x3000;

    /// <summary>
    /// A game with random IDs. Commas in the name break the long name into lines; the short name is the first segment.
    /// </summary>
    /// <param name="name">Display name, commas as line breaks.</param>
    /// <param name="productId">Four-character product ID, or null for a random one.</param>
    /// <param name="gamePad">Advertise GamePad-as-controller use.</param>
    /// <param name="random">Source of IDs; null for a new one.</param>
    /// <exception cref="ArgumentException">Blank name or a product ID that is not four characters.</exception>
    public static Game Create(string name, string? productId = null, bool gamePad = false, Random? random = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (productId is not null && productId.Length != 4)
            throw new ArgumentException("Product ID must be four characters.", nameof(productId));

        random ??= new Random();
        var titleId = new TitleId(TitleType.Demo, (uint)(Half(random) << 16 | Half(random)));
        var groupHalf = Half(random);
        var group = new GroupId((uint)groupHalf);
        var product = new ProductCode(ProductCode.EShop, productId ?? groupHalf.ToString("X4", CultureInfo.InvariantCulture));

        var trimmed = name.Trim();
        var longName = trimmed.Replace(",", "\n");
        var shortName = trimmed.Split(',')[0].Trim();
        return new Game(titleId, group, product)
        {
            Names = LocalizedName.ForAllLanguages(new LocalizedName(shortName, longName)),
            GamePadUse = gamePad ? 65537u : 0u,
        };
    }

    private static int Half(Random random) => random.Next(MinimumIdHalf, 0x10000);
}
