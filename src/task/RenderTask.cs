using livemap.layer.builtin;
using livemap.render;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace livemap.task;

public sealed class RenderTask(LiveMap server, RenderTaskManager renderTaskManager) {
    // let's not create too many of these, so we don't kill the GC
    private readonly BlockPos _mutableBlockPos = new(0);

    public void ScanRegion(int regionX, int regionZ) {
        try {
            ServerMapRegion? region = renderTaskManager.ChunkLoader.GetMapRegion(ChunkPos.ToChunkIndex(regionX, 0, regionZ));
            if (region == null) {
                return;
            }

            // check for existing chunks only in this region
            int chunkX1 = regionX << 4;
            int chunkZ1 = regionZ << 4;
            int chunkX2 = chunkX1 + 16;
            int chunkZ2 = chunkZ1 + 16;

            // get blockdata from all chunks
            IEnumerable<ChunkPos> chunks = renderTaskManager.ChunkLoader.GetAllMapChunkPositions()
                .Where(chunkPos => chunkPos.X >= chunkX1 && chunkPos.Z >= chunkZ1 && chunkPos.X < chunkX2 && chunkPos.Z < chunkZ2);
            BlockData blockData = new();
            foreach (ChunkPos chunkPos in chunks) {
                ScanChunkColumn(region, chunkPos, blockData);
            }

            // process the region through all the renderers
            foreach ((string _, Renderer renderer) in server.RendererRegistry) {
                renderer.AllocateImage(regionX, regionZ);
                renderer.ProcessBlockData(regionX, regionZ, blockData);
                renderer.CalculateShadows();
                renderer.SaveImage();
            }
        } catch (Exception e) {
            Logger.Warn(e.ToString());
        }
    }

    private void ScanChunkColumn(ServerMapRegion region, ChunkPos chunkPos, BlockData blockData) {
        // get chunkmap from game save
        // this is just basic info about a chunk column, like heightmaps
        ServerMapChunk? mapChunk = renderTaskManager.ChunkLoader.GetMapChunk(ChunkPos.ToChunkIndex(chunkPos.X, chunkPos.Y, chunkPos.Z));
        if (mapChunk == null) {
            return;
        }

        // do not render incomplete chunks
        if (mapChunk.CurrentIncompletePass < EnumWorldGenPass.Done) {
            return;
        }

        // check which chunk slices need to be loaded to get the top surface block
        List<int> chunkIndexesToLoad = [];
        FindChunksToLoad(region, mapChunk, chunkPos, chunkIndexesToLoad);

        // load the actual chunks slices from game save
        ServerChunk?[] chunkSlices = new ServerChunk?[server.Sapi.WorldManager.MapSizeY >> 5];
        foreach (int y in chunkIndexesToLoad) {
            chunkSlices[y] = renderTaskManager.ChunkLoader.GetChunk(ChunkPos.ToChunkIndex(chunkPos.X, y, chunkPos.Z));
        }

        int startX = chunkPos.X << 5;
        int startZ = chunkPos.Z << 5;
        int endX = startX + 32;
        int endZ = startZ + 32;

        // scan every block column in the chunk
        for (int x = startX; x < endX; x++) {
            for (int z = startZ; z < endZ; z++) {
                blockData.Set(x & 511, z & 511, ScanBlockColumn(x & 31, z & 31, mapChunk, chunkSlices));
            }
        }

        // process the chunk through all the renderers
        foreach ((string _, Renderer renderer) in server.RendererRegistry) {
            renderer.ScanChunkColumn(chunkPos, blockData);
        }

        // process things from structures
        ProcessStructures(chunkPos, chunkSlices);
    }

    private void FindChunksToLoad(ServerMapRegion region, ServerMapChunk mapChunk, ChunkPos chunkPos, List<int> chunkIndexesToLoad) {
        for (int x = 0; x < 32; x++) {
            for (int z = 0; z < 32; z++) {
                int y = GetTopBlockY(mapChunk, x, z) >> 5;
                chunkIndexesToLoad.AddIfNotExists(y);
                if (y > 0) {
                    chunkIndexesToLoad.AddIfNotExists(y - 1);
                }
            }
        }

        // check which chunk slices need to be loaded to get specific structure data
        region.GeneratedStructures?
            .Where(s =>
                s.Code.Contains("trader") ||
                s.Code.Contains("gates")
            )
            .Where(s =>
                (chunkPos.X << 5) < s.Location.MaxX &&
                (chunkPos.Z << 5) < s.Location.MaxZ &&
                (chunkPos.X << 5) + 32 > s.Location.MinX &&
                (chunkPos.Z << 5) + 32 > s.Location.MinZ
            )
            .Foreach(s => {
                for (int y = s.Location.Y1 >> 5; y <= s.Location.Y2 >> 5; y++) {
                    chunkIndexesToLoad.AddIfNotExists(y);
                }
            });
    }

    private void ProcessStructures(ChunkPos chunkPos, ServerChunk?[] chunkSlices) {
        ulong chunkIndex = chunkPos.ToChunkIndex();

        TradersLayer? tradersLayer = server.LayerRegistry.Traders;
        TranslocatorsLayer? translocatorsLayer = server.LayerRegistry.Translocators;

        if (tradersLayer == null && translocatorsLayer == null) {
            return;
        }

        HashSet<TradersLayer.Trader> traders = [];
        HashSet<TranslocatorsLayer.Translocator> translocators = [];

        int seaLevel = server.Sapi.World.SeaLevel;

        chunkSlices.Foreach(chunk => {
            if (server.Config.Layers.Traders.Enabled && tradersLayer != null) {
                chunk?.Entities.Foreach(entity => {
                    if (entity is not EntityTrader trader) {
                        return;
                    }

                    Vec3i pos = trader.Pos.ToVec3i();
                    if (pos.Y < seaLevel) {
                        return;
                    }

                    long id = trader.EntityId;
                    string type = $"item-creature-{trader.Code.Path}";
                    string name = trader.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name") ?? "Unknown Name";

                    traders.Add(new TradersLayer.Trader(type, id, name, pos));
                    Logger.Debug($"Trader at {pos} is named {name} (type: {type})");
                });
            }

            if (server.Config.Layers.Translocators.Enabled) {
                chunk?.BlockEntities.Values.Foreach(blockEntity => {
                    if (blockEntity is not BlockEntityStaticTranslocator { TargetLocation: not null } tl) {
                        return;
                    }

                    BlockPos pos = tl.Pos;
                    BlockPos loc = tl.TargetLocation;

                    // save tl to file
                    Logger.Debug($"Translocator at {pos} points to {loc}");
                });
            }
        });

        tradersLayer?.SetTraders(chunkIndex, traders);
        translocatorsLayer?.SetTranslocators(chunkIndex, translocators);
    }

    private BlockData.Data ScanBlockColumn(int x, int z, ServerMapChunk mapChunk, ServerChunk?[] chunkSlices) {
        int y = 0;
        int top = 0;
        int under = 0;

        try {
            y = GetTopBlockY(mapChunk, x, z);
            ServerChunk? serverChunk = chunkSlices[y >> 5];
            if (serverChunk != null) {
                top = serverChunk.Data[Mathf.BlockIndex(x, y, z)];
                CheckForMicroBlocks(x, y, z, serverChunk, ref top);
            }

            serverChunk = chunkSlices[(y - 1) >> 5];
            if (serverChunk != null) {
                under = serverChunk.Data[Mathf.BlockIndex(x, y - 1, z)];
                CheckForMicroBlocks(x, y - 1, z, serverChunk, ref under);
            }
        } catch (Exception) {
            // ignore
        }

        return new BlockData.Data(y, top, under);
    }

    private void CheckForMicroBlocks(int x, int y, int z, ServerChunk serverChunk, ref int top) {
        if (!renderTaskManager.MicroBlocks.Contains(top)) {
            return;
        }

        serverChunk.BlockEntities.TryGetValue(_mutableBlockPos.Set(x, y, z), out BlockEntity? be);
        top = be is BlockEntityMicroBlock bemb ? bemb.BlockIds[0] : renderTaskManager.LandBlock;
    }

    private int GetTopBlockY(ServerMapChunk mapChunk, int x, int z) {
        ushort blockY = mapChunk.RainHeightMap[Mathf.BlockIndex(x, z)];
        return GameMath.Clamp(blockY, 0, server.Sapi.WorldManager.MapSizeY - 1);
    }
}
