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

        // Cache for previous row's ProcessBlock results to avoid redundant calculations
        // prevRowCache[z] stores the y value from block at (x-1, z) in previous row
        int?[] prevRowCache = new int?[TileConstants.RegionSize];
        // Cache for current row's ProcessBlock results
        // currentRowCache[z] stores the y value from block at (x, z) in current row
        int?[] currentRowCache = new int?[TileConstants.RegionSize];

        for (int x = 0; x < TileConstants.RegionSize; x++) {
            // Swap caches: current row becomes previous row for next iteration
            (prevRowCache, currentRowCache) = (currentRowCache, prevRowCache);
            // Clear current row cache (it now contains the old prevRowCache, which we'll overwrite)
            Array.Clear(currentRowCache);

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

    /// <summary>
    ///     Optimized shadow calculation that reuses cached neighbor ProcessBlock results
    ///     When processing in row-major order (x outer, z inner), we've already computed:
    ///     - northwest (x-1, z-1): from prevRowCache[z-1] (previous row, column z-1)
    ///     - north (x, z-1): from currentRowCache[z-1] (current row, column z-1)
    ///     - west (x-1, z): from prevRowCache[z] (previous row, column z)
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
