using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace livemap;

public sealed class EventCoordinator : IDisposable {
    private readonly LiveMap _server;
    private readonly long _gameTickTaskId;
    private int _lastMonth = -1;

    public EventCoordinator(LiveMap server) {
        _server = server;

        _server.Sapi.Event.ChunkDirty += OnChunkDirty;
        _server.Sapi.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        _server.Sapi.Event.GameWorldSave += OnGameWorldSave;

        // things to do on first game tick
        _server.Sapi.Event.RegisterCallback(_ => {
            _server.RendererRegistry.RegisterBuiltIns();
            _server.LayerRegistry.RegisterBuiltIns();
            CheckSeason();

            // Force immediate settings.json update after registries are populated
            _server.AsyncTaskManager?.Tick();
        }, 1);

        _gameTickTaskId = _server.Sapi.Event.RegisterGameTickListener(OnGameTick, 1000, 1000);
    }

    private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason) {
        // queue it up, it will process when the game saves
        Logger.Debug("chunk.dirty".ToLang(chunkCoord));
        _server.RenderTaskManager?.Queue(chunkCoord.X >> 4, chunkCoord.Z >> 4);
    }

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks) {
        Logger.Debug("chunk.loaded".ToLang(chunkCoord));
        _server.RenderTaskManager?.Queue(chunkCoord.X >> 4, chunkCoord.Y >> 4);
    }

    private void OnGameWorldSave() {
        // delay a bit to ensure chunks actually save to disk first
        _server.Sapi.Event.RegisterCallback(_ => _server.RenderTaskManager?.ProcessQueue(), 1000);
    }

    // this method ticks every 1000ms on the game thread
    private void OnGameTick(float delta) {
        // ensure render task is running
        //RenderTask.Run();

        // ensure web server is still running
        _server.WebServer?.Run();

        // todo - update player positions, public waypoints, etc
        _server.AsyncTaskManager?.Tick();

        CheckSeason();
    }

    private void CheckSeason() {
        int currentMonth = _server.Sapi.World.Calendar.Month;
        if (currentMonth == _lastMonth) {
            return;
        }

        // Only do full render if it's not the first load (lastMonth != -1)
        bool shouldRender = _lastMonth != -1 && _server.ConfigManager.Config.Render.FullRenderOnSeasonChange;

        _lastMonth = currentMonth;
        Logger.Info($"Season changed to month {currentMonth}. Loading seasonal colormap...");

        _server.Colormap.LoadFromDisk(_server.Sapi.World, currentMonth);

        if (shouldRender) {
            Logger.Info("Triggering full map render due to season change...");
            _server.RenderTaskManager?.QueueAll();
        }
    }

    public void Dispose() {
        _server.Sapi.Event.ChunkDirty -= OnChunkDirty;
        _server.Sapi.Event.ChunkColumnLoaded -= OnChunkColumnLoaded;
        _server.Sapi.Event.GameWorldSave -= OnGameWorldSave;

        _server.Sapi.Event.UnregisterGameTickListener(_gameTickTaskId);
    }
}
