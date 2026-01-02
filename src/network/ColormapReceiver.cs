using System.Collections.Concurrent;
using livemap.data;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace livemap.network;

/// <summary>
/// Manages reassembly of chunked colormap transfers from clients.
/// </summary>
public sealed class ColormapReceiver : IDisposable {
    /// <summary>
    /// Timeout in milliseconds for incomplete transfers.
    /// </summary>
    private const int TransferTimeoutMs = 60000; // 60 seconds

    private readonly LiveMap _server;
    private readonly ConcurrentDictionary<string, ChunkedTransfer> _activeTransfers = new();
    private readonly long _cleanupTaskId;

    public ColormapReceiver(LiveMap server) {
        _server = server;
        // Register cleanup task to run every 30 seconds
        _cleanupTaskId = server.Sapi.Event.RegisterGameTickListener(_ => CleanupStaleTransfers(), 30000);
    }

    /// <summary>
    /// Handles an incoming chunk packet from a player.
    /// </summary>
    public void ReceiveChunk(IServerPlayer player, ColormapChunkPacket chunk) {
        if (!player.HasPrivilege(Privilege.root)) {
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.error.no-privilege".ToLang(), EnumChatType.CommandError);
            Logger.Warn("colormap.invalid-privilege".ToLang(player.PlayerName));
            return;
        }

        // Validate TotalChunks is positive to prevent array initialization issues. Limit to at most 64MB of colormap (this is overkill)
        if (chunk.TotalChunks <= 0 || chunk.TotalChunks > 1024) {
            Logger.Warn("colormap.invalid-size".ToLang(chunk.TotalChunks, player.PlayerName));
            return;
        }

        ChunkedTransfer transfer = _activeTransfers.GetOrAdd(chunk.TransferId, _ => new ChunkedTransfer {
            PlayerId = player.PlayerUID,
            PlayerName = player.PlayerName,
            TotalChunks = chunk.TotalChunks,
            ReceivedChunks = new byte[chunk.TotalChunks][],
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Month = chunk.Month
        });

        // Validate transfer belongs to this player
        if (transfer.PlayerId != player.PlayerUID) {
            Logger.Warn("colormap.wrong-player".ToLang(player.PlayerName));
            return;
        }

        // Validate TotalChunks consistency to prevent malformed transfers
        if (chunk.TotalChunks != transfer.TotalChunks) {
            Logger.Warn("colormap.total-mismatch".ToLang(player.PlayerName, transfer.TotalChunks, chunk.TotalChunks));
            _activeTransfers.TryRemove(chunk.TransferId, out _);
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.error".ToLang(), EnumChatType.CommandError);
            return;
        }

        // Store the chunk
        if (chunk.ChunkIndex >= 0 && chunk.ChunkIndex < transfer.TotalChunks) {
            // Validate chunk size to prevent memory abuse (max 64KB + small buffer)
            if (chunk.Data.Length > 70000) {
                Logger.Warn("colormap.too-big".ToLang(chunk.ChunkIndex, player.PlayerName, chunk.Data.Length));
                _activeTransfers.TryRemove(chunk.TransferId, out _);
                player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.error".ToLang(), EnumChatType.CommandError);
                return;
            }

            bool isComplete;
            lock (transfer.SyncLock) {
                // Only increment counter if this is a new chunk, not a duplicate
                if (transfer.ReceivedChunks[chunk.ChunkIndex] == null) {
                    transfer.ChunksReceived++;
                }

                transfer.ReceivedChunks[chunk.ChunkIndex] = chunk.Data;

                // Check completion atomically and set flag to prevent duplicate processing
                isComplete = transfer.ChunksReceived == transfer.TotalChunks && !transfer.IsCompleted;
                if (isComplete) {
                    transfer.IsCompleted = true;
                }
            }

            Logger.Debug("colormap.received-chunk".ToLang(chunk.ChunkIndex + 1, chunk.TotalChunks, player.PlayerName));

            // Check if transfer is complete
            if (isComplete) {
                CompleteTransfer(player, chunk.TransferId, transfer);
            }
        } else {
            // Invalid chunk index - log warning and invalidate the entire transfer
            Logger.Warn("colormap.invalid-index".ToLang(chunk.ChunkIndex, transfer.TotalChunks, player.PlayerName));
            _activeTransfers.TryRemove(chunk.TransferId, out _);
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.error".ToLang(), EnumChatType.CommandError);
        }
    }

    private void CompleteTransfer(IServerPlayer player, string transferId, ChunkedTransfer transfer) {
        _activeTransfers.TryRemove(transferId, out _);

        try {
            // Reassemble the data
            int totalLength = transfer.ReceivedChunks.Sum(c => c?.Length ?? 0);
            byte[] reassembledData = new byte[totalLength];
            int offset = 0;

            foreach (byte[] chunk in transfer.ReceivedChunks.Where(c => c != null)!) {
                Array.Copy(chunk, 0, reassembledData, offset, chunk.Length);
                offset += chunk.Length;
            }

            // Convert back to base64 and create packet for processing
            string base64 = Convert.ToBase64String(reassembledData);
            ColormapPacket packet = new() { RawBase64String = base64, Month = transfer.Month };

            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.received".ToLang(), EnumChatType.CommandSuccess);
            Logger.Info("colormap.received-with-chunks".ToLang(player.PlayerName, transfer.TotalChunks));

            _server.Colormap.LoadFromPacket(_server.Sapi.World, packet);
        } catch (Exception e) {
            Logger.Error("colormap.failed-reassembly".ToLang(player.PlayerName, e));
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.error".ToLang(), EnumChatType.CommandError);
        }
    }

    private void CleanupStaleTransfers() {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        List<string> staleTransfers = _activeTransfers
            .Where(kvp => now - kvp.Value.StartTime > TransferTimeoutMs)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string transferId in staleTransfers) {
            if (_activeTransfers.TryRemove(transferId, out ChunkedTransfer? transfer)) {
                Logger.Warn("colormap.timeout".ToLang(transfer.PlayerName, transfer.ChunksReceived, transfer.TotalChunks));
            }
        }
    }

    public void Dispose() {
        _server.Sapi.Event.UnregisterGameTickListener(_cleanupTaskId);
        _activeTransfers.Clear();
    }

    private sealed class ChunkedTransfer {
        public object SyncLock { get; } = new();
        public required string PlayerId { get; init; }
        public required string PlayerName { get; init; }
        public required int TotalChunks { get; init; }
        public required byte[][] ReceivedChunks { get; init; }
        public required long StartTime { get; init; }
        public required int Month { get; init; }
        public int ChunksReceived { get; set; }
        public bool IsCompleted { get; set; }
    }
}
