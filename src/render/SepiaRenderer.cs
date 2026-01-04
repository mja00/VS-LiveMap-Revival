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

        // Cache for previous row's ProcessBlock results to avoid redundant calculations
        int?[] prevRowCache = new int?[TileConstants.RegionSize];
        // Cache for current row's ProcessBlock results
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

    /// <summary>
    ///     Optimized shadow calculation that reuses cached neighbor ProcessBlock results
    /// </summary>
    private float ProcessShadowOptimized(int x, int y, int z, BlockData blockData, int?[] prevRowCache, int?[] currentRowCache) {
        // Get northwest: (x-1, z-1) - from previous row cache
        int northwest = y;
        if (x > 0 && z > 0 && prevRowCache[z - 1].HasValue) {
            northwest = prevRowCache[z - 1]!.Value;
        } else if (x > 0 && z > 0) {
            BlockData.Data? nwBlock = blockData.Get(x - 1, z - 1);
            if (nwBlock != null) {
                (int _, int nwY) = ProcessBlock(nwBlock);
                northwest = nwY;
            }
        }

        // Get north: (x, z-1) - from current row cache
        int north = y;
        if (z > 0 && currentRowCache[z - 1].HasValue) {
            north = currentRowCache[z - 1]!.Value;
        } else if (z > 0) {
            BlockData.Data? nBlock = blockData.Get(x, z - 1);
            if (nBlock != null) {
                (int _, int nY) = ProcessBlock(nBlock);
                north = nY;
            }
        }

        // Get west: (x-1, z) - from previous row cache
        int west = y;
        if (x > 0 && prevRowCache[z].HasValue) {
            west = prevRowCache[z]!.Value;
        } else if (x > 0) {
            BlockData.Data? wBlock = blockData.Get(x - 1, z);
            if (wBlock != null) {
                (int _, int wY) = ProcessBlock(wBlock);
                west = wY;
            }
        }

        int direction = Math.Sign(y - northwest) + Math.Sign(y - north) + Math.Sign(y - west);
        int steepness = Math.Max(Math.Max(Math.Abs(y - northwest), Math.Abs(y - north)), Math.Abs(y - west));
        float slopeFactor = Math.Min(0.5F, steepness / 10F) / 1.25F;
        return direction switch {
            > 0 => 1.08F + slopeFactor,
            < 0 => 0.92F - slopeFactor,
            _ => 1
        };
    }
}
