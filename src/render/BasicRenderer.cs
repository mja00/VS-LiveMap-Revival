using livemap.data;
using livemap.util;
using Vintagestory.API.MathTools;

namespace livemap.render;

/// <summary>
///     Basic renderer with optimized shadow calculation using neighbor caching.
///     Note: Parallel processing (Parallel.For) could provide additional speedup on multi-core systems,
///     but would require removing the shadow cache optimization since it depends on sequential row processing.
///     SetBlockColor is thread-safe (writes to different memory locations per coordinate).
/// </summary>
public class BasicRenderer() : Renderer("basic") {
    public override void ProcessBlockData(int regionX, int regionZ, BlockData blockData) {
        if (TileImage == null) {
            return;
        }

        // Cache colormap reference to avoid repeated property access
        Colormap colormap = LiveMap.Api.Colormap;

        // Cache of y-values from the previous row's ProcessBlock computations to avoid redundant calculations
        // prevRowCache[z] stores the y value from block at (x-1, z) in previous row
        int?[] prevRowCache = new int?[TileConstants.RegionSize];
        // Cache for current row's ProcessBlock results
        // currentRowCache[z] stores the y value from block at (x, z) in current row
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

                uint color = 0;
                if (colormap.TryGet(id, out uint[]? colors)) {
                    color = colors[GameMath.MurmurHash3Mod(x, y, z, colors.Length)];
                }

                // Optimize shadow calculation by reusing cached neighbor values
                float yDiff = ProcessShadowOptimized(x, y, z, blockData, prevRowCache, currentRowCache);

                TileImage.SetBlockColor(x, z, color, yDiff);

                // Update current row cache
                currentRowCache[z] = y;
            }
        }
    }
}
