namespace livemap.render;

public class SepiaRenderer() : Renderer("sepia") {
    public static bool IsWater(int? id) => id == null || LiveMap.Api.SepiaColors.BlockIsWater[(int)id];
    public static byte GetIndex(int id) => LiveMap.Api.SepiaColors.Block2Color[id];
    public static uint GetColor(string id) => LiveMap.Api.SepiaColors.ColorsByCode[id];

    public static uint GetColor(int index) {
        return index >= 0 && index < LiveMap.Api.SepiaColors.ColorsByCode.Count
            ? LiveMap.Api.SepiaColors.ColorsByCode.GetValueAtIndex(index)
            : GetColor("ocean");
    }

    public override void ProcessBlockData(int regionX, int regionZ, BlockData blockData) {
        if (TileImage == null) {
            return;
        }

        for (int x = 0; x < 512; x++) {
            for (int z = 0; z < 512; z++) {
                BlockData.Data? block = blockData.Get(x, z);
                if (block == null) {
                    continue;
                }

                (int id, int y) = ProcessBlock(block);

                uint color = IsWater(id)
                    ? IsWater(blockData.Get(x, z - 1)?.Top) &&
                      IsWater(blockData.Get(x + 1, z)?.Top) &&
                      IsWater(blockData.Get(x, z + 1)?.Top) &&
                      IsWater(blockData.Get(x - 1, z)?.Top)
                        ? GetColor(GetIndex(id))
                        : GetColor("wateredge")
                    : GetColor(GetIndex(id));

                float yDiff = ProcessShadow(x, y, z, blockData);

                TileImage.SetBlockColor(x, z, color, yDiff);
            }
        }
    }
}
