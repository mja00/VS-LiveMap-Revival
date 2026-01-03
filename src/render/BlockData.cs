using livemap.util;

namespace livemap.render;

public class BlockData {
    private readonly Data[] _data = new Data[TileConstants.RegionBlockCount];

    public Data? Get(int x, int z) {
        if (x is < 0 or > TileConstants.RegionMaxIndex || z is < 0 or > TileConstants.RegionMaxIndex) {
            // todo - i really want to get the edge data from the neighbor regions..
            return null;
        }

        return _data.GetValue(Index(x, z)) as Data;
    }

    public void Set(int x, int z, Data data) => _data[Index(x, z)] = data;

    private static int Index(int x, int z) => ((z & TileConstants.RegionMask) * TileConstants.RegionSize) + (x & TileConstants.RegionMask);

    public class Data(int y, int top, int under) {
        public Dictionary<string, object?> Custom = [];
        public int Y { get; } = y;
        public int Top { get; } = top;
        public int Under { get; } = under;
    }
}
