using SkiaSharp;
using Vintagestory.API.Common;

namespace livemap.data;

public class SepiaColors {
    public SepiaColors(LiveMap server) {
        int max = server.Sapi.World.Blocks.Count;
        Block2Color = new byte[max + 1];
        BlockIsWater = new bool[max + 1];

        foreach (KeyValuePair<string, string> val in HexColorsByCode) {
            ColorsByCode[val.Key] = (uint)SKColor.Parse(val.Value);
        }

        foreach (Block block in server.Sapi.World.Blocks) {
            if (block.BlockMaterial == EnumBlockMaterial.Snow && block.Code.Path.Contains("snowblock")) {
                Block2Color[block.BlockId] = (byte)ColorsByCode.IndexOfKey("glacier");
                BlockIsWater[block.BlockId] = false;
                continue;
            }

            string colorCode = "land";
            if (block.Attributes != null) {
                colorCode = block.Attributes["mapColorCode"].AsString() ?? GetDefaultMapColorCode(block.BlockMaterial);
            }

            Block2Color[block.BlockId] = (byte)ColorsByCode.IndexOfKey(colorCode);
            BlockIsWater[block.BlockId] = block.BlockMaterial == EnumBlockMaterial.Liquid || (block.BlockMaterial == EnumBlockMaterial.Ice && block.Code.Path != "glacierice");
        }
    }

    public Vintagestory.API.Datastructures.OrderedDictionary<string, string> HexColorsByCode { get; } = new() {
        { "ink", "#483018" },
        { "settlement", "#856844" },
        { "wateredge", "#483018" },
        { "land", "#AC8858" },
        { "desert", "#C4A468" },
        { "forest", "#98844C" },
        { "road", "#805030" },
        { "plant", "#808650" },
        { "lake", "#CCC890" },
        { "ocean", "#CCC890" },
        { "glacier", "#E0E0C0" }
    };

    public Vintagestory.API.Datastructures.OrderedDictionary<string, uint> ColorsByCode { get; } = [];

    public byte[] Block2Color { get; private set; }
    public bool[] BlockIsWater { get; private set; }

    public static string GetDefaultMapColorCode(EnumBlockMaterial material) {
        return material switch {
            EnumBlockMaterial.Soil => "land",
            EnumBlockMaterial.Sand => "desert",
            EnumBlockMaterial.Ore => "land",
            EnumBlockMaterial.Gravel => "desert",
            EnumBlockMaterial.Stone => "land",
            EnumBlockMaterial.Leaves => "forest",
            EnumBlockMaterial.Plant => "plant",
            EnumBlockMaterial.Wood => "forest",
            EnumBlockMaterial.Snow => "glacier",
            EnumBlockMaterial.Liquid => "lake",
            EnumBlockMaterial.Ice => "glacier",
            EnumBlockMaterial.Lava => "lava",
            _ => "land"
        };
    }

    public void Dispose() {
        ColorsByCode.Clear();
        HexColorsByCode.Clear();
        Block2Color = [];
        BlockIsWater = [];
    }
}
