namespace livemap.util;

public static class TileConstants {
    /// <summary>
    ///     The size of a region/tile in blocks (512x512)
    /// </summary>
    public const int RegionSize = 512;

    /// <summary>
    ///     Maximum index for region coordinates (RegionSize - 1)
    /// </summary>
    public const int RegionMaxIndex = RegionSize - 1;

    /// <summary>
    ///     Bitmask for wrapping coordinates within a region
    /// </summary>
    public const int RegionMask = RegionMaxIndex;

    /// <summary>
    ///     Total number of blocks in a region (RegionSize * RegionSize)
    /// </summary>
    public const int RegionBlockCount = RegionSize * RegionSize;

    /// <summary>
    ///     Bit shift value for region size (log2(512) = 9)
    /// </summary>
    public const int RegionSizeBitShift = 9;
}
