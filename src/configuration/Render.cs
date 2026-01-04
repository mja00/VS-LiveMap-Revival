namespace livemap.configuration;

public class Render {
    public bool FullRenderOnSeasonChange { get; set; } = true;

    /// <summary>
    ///     Size of the chunk cache (number of chunks to cache in memory).
    ///     Larger values use more memory but provide better performance for frequently accessed chunks.
    ///     Default: 1000 chunks
    /// </summary>
    public int ChunkCacheSize { get; set; } = 1000;

    /// <summary>
    ///     Enable incremental tile saving (only save zoom levels that changed).
    ///     When enabled, unchanged zoom levels are skipped, reducing file I/O.
    ///     Default: true
    /// </summary>
    public bool EnableIncrementalSaves { get; set; } = true;
}
