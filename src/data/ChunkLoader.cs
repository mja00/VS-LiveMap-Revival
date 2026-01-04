using System.Data;
using System.Data.Common;
using livemap.util;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace livemap.data;

public class ChunkLoader {
    private readonly ChunkDataPool _chunkDataPool;
    private readonly ServerMain _server;
    private readonly SqliteConnection _sqliteConn;
    private readonly LRUCache<ulong, ServerMapChunk> _mapChunkCache;
    private readonly LRUCache<ulong, ServerChunk> _chunkCache;
    private readonly LRUCache<ulong, ServerMapRegion> _regionCache;

    public ChunkLoader(ICoreServerAPI api, int chunkCacheSize = 1000) {
        _server = (api.World as ServerMain)!;
        // do not use server's connection, create our own to prevent concurrency issues
        (_sqliteConn = new SqliteConnection(new DbConnectionStringBuilder {
            {
                "Data Source", _server
                    .GetField<ChunkServerThread>("chunkThread")!
                    .GetField<GameDatabase>("gameDatabase")!
                    .GetField<SQLiteDBConnection>("conn")!
                    .GetField<string>("databaseFileName")!
            },
            { "Pooling", "false" },
            { "Mode", "ReadOnly" }
        }.ToString())).Open();
        _chunkDataPool = new ChunkDataPool(32, _server);

        // Initialize LRU caches
        _mapChunkCache = new LRUCache<ulong, ServerMapChunk>(chunkCacheSize);
        _chunkCache = new LRUCache<ulong, ServerChunk>(chunkCacheSize);
        _regionCache = new LRUCache<ulong, ServerMapRegion>(100); // Smaller cache for regions
    }

    public IEnumerable<ChunkPos> GetAllMapRegionPositions() {
        using SqliteCommand sqlite = _sqliteConn.CreateCommand();
        sqlite.CommandText = "SELECT position FROM mapregion";
        using SqliteDataReader reader = sqlite.ExecuteReader();

        // Materialize to a list
        List<ChunkPos> positions = [];
        while (reader.Read()) {
            positions.Add(ChunkPos.FromChunkIndex_saveGamev2((ulong)(long)reader["position"]));
        }

        return positions;
    }

    public IEnumerable<ChunkPos> GetAllMapChunkPositions() => GetAllMapPositions("chunk");

    private IEnumerable<ChunkPos> GetAllMapPositions(string type) {
        using SqliteCommand sqlite = _sqliteConn.CreateCommand();
        sqlite.CommandText = $"SELECT position FROM map{type}";
        using SqliteDataReader reader = sqlite.ExecuteReader();

        // Materialize to a list
        List<ChunkPos> positions = new();
        while (reader.Read()) {
            positions.Add(ChunkPos.FromChunkIndex_saveGamev2((ulong)(long)reader["position"]));
        }

        return positions;
    }

    public ServerMapRegion? GetMapRegion(ulong position) {
        // Check cache first
        if (_regionCache.TryGet(position, out var cachedRegion)) {
            return cachedRegion;
        }

        byte[]? regionData = GetTableData(position, "mapregion");
        if (regionData == null) {
            return null;
        }

        ServerMapRegion region = ServerMapRegion.FromBytes(regionData);
        _regionCache.Add(position, region);
        return region;
    }

    public ServerMapChunk? GetMapChunk(ulong position) {
        // Check cache first
        if (_mapChunkCache.TryGet(position, out var cachedChunk)) {
            return cachedChunk;
        }

        byte[]? chunkData = GetTableData(position, "mapchunk");
        if (chunkData == null) {
            return null;
        }

        ServerMapChunk chunk = ServerMapChunk.FromBytes(chunkData);
        _mapChunkCache.Add(position, chunk);
        return chunk;
    }

    public ServerChunk? GetChunk(ulong position) {
        // Check cache first
        if (_chunkCache.TryGet(position, out var cachedChunk)) {
            return cachedChunk;
        }

        byte[]? chunkData = GetTableData(position, "chunk");
        if (chunkData == null) {
            return null;
        }

        ServerChunk chunk = ServerChunk.FromBytes(chunkData, _chunkDataPool, _server);
        chunk.Unpack_ReadOnly();
        _chunkCache.Add(position, chunk);
        return chunk;
    }

    /// <summary>
    ///     Clears all caches. Should be called when world data changes (e.g., after world save).
    /// </summary>
    public void ClearCache() {
        _regionCache.Clear();
        _mapChunkCache.Clear();
        _chunkCache.Clear();
    }

    private byte[]? GetTableData(ulong index, string name) {
        using SqliteCommand sqlite = _sqliteConn.CreateCommand();
        sqlite.CommandText = $"SELECT data FROM {name} WHERE position=@pos";
        sqlite.Parameters.Add(new SqliteParameter { ParameterName = "pos", DbType = DbType.UInt64, Value = index });
        using SqliteDataReader reader = sqlite.ExecuteReader();
        return reader.Read() ? reader["data"] as byte[] : null;
    }

    public void Dispose() {
        try {
            _chunkDataPool.SlowDispose();
            _sqliteConn.Close();
        } catch (Exception e) {
            Logger.Warn($"Failed to dispose ChunkLoader: {e.Message}");
        }
    }
}
