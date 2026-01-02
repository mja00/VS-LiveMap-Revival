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
    private readonly ServerMain _server;
    private readonly SqliteConnection _sqliteConn;
    private readonly ChunkDataPool _chunkDataPool;

    public ChunkLoader(ICoreServerAPI api) {
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
    }

    public IEnumerable<ChunkPos> GetAllMapRegionPositions() {
        return GetAllMapPositions("region");
    }

    public IEnumerable<ChunkPos> GetAllMapChunkPositions() {
        return GetAllMapPositions("chunk");
    }

    private IEnumerable<ChunkPos> GetAllMapPositions(string type) {
        using SqliteCommand sqlite = _sqliteConn.CreateCommand();
        sqlite.CommandText = $"SELECT position FROM map{type}";
        using SqliteDataReader reader = sqlite.ExecuteReader();

        // Materialize to a list
        var positions = new List<ChunkPos>();
        while (reader.Read()) {
            positions.Add(ChunkPos.FromChunkIndex_saveGamev2((ulong)(long)reader["position"]));
        }
        return positions;
    }

    public ServerMapRegion? GetMapRegion(ulong position) {
        byte[]? regionData = GetTableData(position, "mapregion");
        return regionData == null ? null : ServerMapRegion.FromBytes(regionData);
    }

    public ServerMapChunk? GetMapChunk(ulong position) {
        byte[]? chunkData = GetTableData(position, "mapchunk");
        return chunkData == null ? null : ServerMapChunk.FromBytes(chunkData);
    }

    public ServerChunk? GetChunk(ulong position) {
        byte[]? chunkData = GetTableData(position, "chunk");
        ServerChunk? chunk = chunkData == null ? null : ServerChunk.FromBytes(chunkData, _chunkDataPool, _server);
        chunk?.Unpack_ReadOnly();
        return chunk;
    }

    private byte[]? GetTableData(ulong index, string name) {
        using SqliteCommand sqlite = _sqliteConn.CreateCommand();
        sqlite.CommandText = $"SELECT data FROM {name} WHERE position=@pos";
        sqlite.Parameters.Add(new SqliteParameter {
            ParameterName = "pos",
            DbType = DbType.UInt64,
            Value = index
        });
        using SqliteDataReader reader = sqlite.ExecuteReader();
        return reader.Read() ? reader["data"] as byte[] : null;
    }

    public void Dispose() {
        try {
            _chunkDataPool.SlowDispose();
            _sqliteConn.Close();
        } catch (Exception) {
            // ignore
        }
    }
}
