using livemap.data;
using livemap.util;
using Vintagestory.API.MathTools;

namespace livemap.render;

public class BasicRenderer() : Renderer("basic") {
    public override void ProcessBlockData(int regionX, int regionZ, BlockData blockData) {
        if (TileImage == null) {
            return;
        }

        // Cache colormap reference to avoid repeated property access
        Colormap colormap = LiveMap.Api.Colormap;

        for (int x = 0; x < TileConstants.RegionSize; x++) {
            for (int z = 0; z < TileConstants.RegionSize; z++) {
                BlockData.Data? block = blockData.Get(x, z);
                if (block == null) {
                    continue;
                }

                (int id, int y) = ProcessBlock(block);

                uint color = 0;
                if (colormap.TryGet(id, out uint[]? colors)) {
                    color = colors[GameMath.MurmurHash3Mod(x, y, z, colors.Length)];
                }

                float yDiff = ProcessShadow(x, y, z, blockData);

                TileImage.SetBlockColor(x, z, color, yDiff);
            }
        }
    }
}
