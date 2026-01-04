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

    public virtual void Dispose() => TileImage?.Dispose();
}
