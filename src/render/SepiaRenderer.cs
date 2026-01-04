using livemap.util;

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

        // Cache for previous row's block heights (y-values) used by ProcessShadowOptimized to avoid redundant calculations
        int?[] prevRowCache = new int?[TileConstants.RegionSize];
        // Cache for current row's block heights (y-values) used by ProcessShadowOptimized
        int?[] currentRowCache = new int?[TileConstants.RegionSize];

        for (int x = 0; x < TileConstants.RegionSize; x++) {
            // Swap caches: current row becomes previous row for next iteration
            (prevRowCache, currentRowCache) = (currentRowCache, prevRowCache);

            for (int z = 0; z < TileConstants.RegionSize; z++) {
                BlockData.Data? block = blockData.Get(x, z);
                if (block == null) {
                    currentRowCache[z] = null;
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

                // Use optimized shadow calculation
                float yDiff = ProcessShadowOptimized(x, y, z, blockData, prevRowCache, currentRowCache);

                TileImage.SetBlockColor(x, z, color, yDiff);

                // Update current row cache
                currentRowCache[z] = y;
            }
        }
    }
}
