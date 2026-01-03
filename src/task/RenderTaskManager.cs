using System.Collections.Concurrent;
using livemap.data;
using livemap.util;
using Vintagestory.API.Common;

namespace livemap.task;

public sealed class RenderTaskManager {
    private readonly LiveMap _server;

    public readonly ChunkLoader ChunkLoader;
    public RenderTask RenderTask { get; }

    public HashSet<int> MicroBlocks { get; }
    public HashSet<int> BlocksToIgnore { get; }
    public int LandBlock { get; }

    private readonly ConcurrentQueue<long> _bufferQueue = new();
    private readonly BlockingCollection<long> _processQueueHigh = [];
    private readonly BlockingCollection<long> _processQueueLow = [];

    private Thread? _thread;
    private bool _running;
    private bool _stopped;

    public bool IsRunning => _running;

    public RenderTaskManager(LiveMap server) {
        _server = server;

        ChunkLoader = new ChunkLoader(server.Sapi);
        RenderTask = new RenderTask(server, this);

        MicroBlocks = server.Sapi.World.Blocks
            .Where(block => block.Code != null)
            .Where(block =>
                block.Code.Path.StartsWith("chiseledblock") ||
                block.Code.Path.StartsWith("microblock"))
            .Select(block => block.Id)
            .ToHashSet();

        BlocksToIgnore = server.Sapi.World.Blocks
            .Where(block => block.Code != null)
            .Where(block =>
                (block.Code.Path.EndsWith("-snow") && !MicroBlocks.Contains(block.Id)) ||
                block.Code.Path.EndsWith("-snow2") ||
                block.Code.Path.EndsWith("-snow3") ||
                block.Code.Path.Equals("snowblock") ||
                block.Code.Path.Contains("snowlayer-"))
            .Select(block => block.Id).ToHashSet();

        LandBlock = server.Sapi.World.GetBlock(new AssetLocation("game", "soil-low-normal")).Id;
    }

    public void Queue(int regionX, int regionZ) {
        if (_stopped) {
            return;
        }

        // convert region coordinates to long
        long index = Mathf.AsLong(regionX, regionZ);

        // ensure this region hasn't already been queued up
        bool inHigh = _processQueueHigh.Contains(index);
        bool inLow = _processQueueLow.Contains(index);

        if (_bufferQueue.Contains(index) || inHigh || inLow) {
            return;
        }

        // queue it up to the buffer, so it doesn't get process immediately
        _bufferQueue.Enqueue(index);

        Logger.Debug($"Queueing region {regionX},{regionZ} (buffer: {_bufferQueue.Count} high:{_processQueueHigh.Count} low:{_processQueueLow.Count})");
    }

    public void QueueAll() {
        if (_stopped) {
            return;
        }

        HashSet<long> existing = [.. _bufferQueue];
        foreach (long region in _processQueueHigh) {
            existing.Add(region);
        }

        foreach (long region in _processQueueLow) {
            existing.Add(region);
        }

        int count = 0;
        foreach (long index in ChunkLoader.GetAllMapRegionPositions().Select(pos => Mathf.AsLong(pos.X, pos.Z))) {
            if (existing.Contains(index)) {
                continue;
            }

            _bufferQueue.Enqueue(index);
            count++;
        }

        Logger.Info($"Queued {count} regions for full render.");
        ProcessQueue();
    }

    public void ProcessQueue() {
        if (_stopped) {
            Logger.Debug("ProcessQueue skipped: Stopped");
            return;
        }

        // we need a colormap
        if (_server.Colormap.Count == 0) {
            Logger.Warn("Cannot process render queue. No colormap loaded");
            return;
        }

        // pass all regions from buffer queue to the High priority process queue
        // (Buffer implies recent event, so likely high priority)
        while (_bufferQueue.TryDequeue(out long region)) {
            _processQueueHigh.Add(region);
        }

        if (_processQueueHigh.Count > 0 || _processQueueLow.Count > 0) {
            Logger.Debug($"ProcessQueue moved items. High: {_processQueueHigh.Count}, Low: {_processQueueLow.Count}");
        }

        if (_running) {
            // this task is still running, no need to restart it
            return;
        }

        _running = true;

        (_thread = new Thread(_ => {
            try {
                BlockingCollection<long>[] queues = [_processQueueHigh, _processQueueLow];
                while (_running) {
                    int queueIndex = BlockingCollection<long>.TakeFromAny(queues, out long region);

                    if (queueIndex == 1 && _processQueueHigh.TryTake(out long highPriorityRegion)) {
                        ProcessRegion(highPriorityRegion);
                    }

                    ProcessRegion(region);
                }
            } catch (Exception) {
                // ignore
            }

            _running = false;
        })).Start();
    }

    private void ProcessRegion(long region) {
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        int regionX = Mathf.LongToX(region);
        int regionZ = Mathf.LongToZ(region);

        RenderTask.ScanRegion(regionX, regionZ);

        long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        Logger.Debug($"Region {regionX},{regionZ} finished ({end - start}ms) - Remaining High: {_processQueueHigh.Count}, Low: {_processQueueLow.Count}");
    }

    public void Dispose() {
        bool cancelled = !_stopped && _running;

        _stopped = true;

        _thread?.Interrupt();
        _thread = null;

        _bufferQueue.Clear();
        _bufferQueue.Clear();
        while (_processQueueHigh.TryTake(out _)) { }

        while (_processQueueLow.TryTake(out _)) { }

        if (cancelled) {
            Logger.Warn("Render task cancelled");
        }

        MicroBlocks.Clear();
        BlocksToIgnore.Clear();

        ChunkLoader.Dispose();
    }

    public (int, int) GetCounts() => (_bufferQueue.Count, _processQueueHigh.Count + _processQueueLow.Count);
}
