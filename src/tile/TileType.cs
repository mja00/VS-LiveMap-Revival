using SkiaSharp;

namespace livemap.tile;

public class TileType {
    public static readonly Vintagestory.API.Datastructures.OrderedDictionary<string, TileType> Types = [];

    public static readonly TileType Png = Register(new TileType("png", SKEncodedImageFormat.Png));
    public static readonly TileType Webp = Register(new TileType("webp", SKEncodedImageFormat.Webp));

    private TileType(string type, SKEncodedImageFormat format) {
        Type = type;
        Format = format;
    }

    public string Type { get; }

    public SKEncodedImageFormat Format { get; }

    private static TileType Register(TileType tileType) {
        Types.Add(tileType.Type, tileType);
        return tileType;
    }

    public override string ToString() => Type;
}
