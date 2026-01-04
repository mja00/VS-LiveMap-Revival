using System.Runtime.CompilerServices;
using livemap.registry;
using livemap.tile;
using Vintagestory.Common.Database;

namespace livemap.render;

public abstract class Renderer(string id) : Keyed {
    public TileImage? TileImage { get; set; }
    public string Id { get; } = id;

    /// <summary>
    ///     Cached reference to BlocksToIgnore HashSet to avoid repeated property access
    /// </summary>
    protected HashSet<int>? BlocksToIgnore { get; set; }

    /// <summary>
    ///     Initialize renderer with server context (called after registration)
    /// </summary>
    public virtual void Initialize(LiveMap server) {
        BlocksToIgnore = server.RenderTaskManager?.BlocksToIgnore;
    }

    public virtual void AllocateImage(int regionX, int regionZ) => TileImage = new TileImage(regionX, regionZ);

    public virtual void SaveImage() => TileImage?.Save(Id);

    public virtual void CalculateShadows() => TileImage?.CalculateShadows();

    public virtual void ScanChunkColumn(ChunkPos chunkPos, BlockData blockData) {
    }

    public virtual void ProcessBlockData(int regionX, int regionZ, BlockData blockData) {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual (int, int) ProcessBlock(BlockData.Data? block, int defY = 0) {
        if (block == null) {
            return (0, defY);
        }

        int id, y;
        if (BlocksToIgnore?.Contains(block.Top) ?? false) {
            id = block.Under;
            y = block.Y - 1;
        } else {
            id = block.Top;
            y = block.Y;
        }

        return (id, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual float ProcessShadow(int x, int y, int z, BlockData blockData) {
        (int _, int northwest) = ProcessBlock(blockData.Get(x - 1, z - 1), y);
        (int _, int north) = ProcessBlock(blockData.Get(x, z - 1), y);
        (int _, int west) = ProcessBlock(blockData.Get(x - 1, z), y);

        int direction = Math.Sign(y - northwest) + Math.Sign(y - north) + Math.Sign(y - west);
        int steepness = Math.Max(Math.Max(Math.Abs(y - northwest), Math.Abs(y - north)), Math.Abs(y - west));
        float slopeFactor = Math.Min(0.5F, steepness / 10F) / 1.25F;
        return direction switch {
            > 0 => 1.08F + slopeFactor,
            < 0 => 0.92F - slopeFactor,
            _ => 1
        };
    }

    /// <summary>
    ///     Optimized shadow calculation that reuses cached neighbor ProcessBlock results.
    ///     When processing in row-major order (x outer, z inner), we've already computed:
    ///     - northwest (x-1, z-1): from prevRowCache[z-1] (previous row, column z-1)
    ///     - north (x, z-1): from currentRowCache[z-1] (current row, column z-1)
    ///     - west (x-1, z): from prevRowCache[z] (previous row, column z)
    /// </summary>
    /// <param name="x">X coordinate of the current block</param>
    /// <param name="y">Y coordinate (height) of the current block</param>
    /// <param name="z">Z coordinate of the current block</param>
    /// <param name="blockData">Block data for the region</param>
    /// <param name="prevRowCache">Cache of y-values from the previous row (x-1)</param>
    /// <param name="currentRowCache">Cache of y-values from the current row (x)</param>
    /// <returns>Shadow factor for the block</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float ProcessShadowOptimized(int x, int y, int z, BlockData blockData, int?[] prevRowCache, int?[] currentRowCache) {
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

    public virtual void Dispose() => TileImage?.Dispose();
}
