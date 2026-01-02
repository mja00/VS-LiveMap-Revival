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
            Logger.Warn($"Ignoring colormap chunk from non-privileged user {player.PlayerName}");
            return;
        }

        // Validate TotalChunks is positive to prevent array initialization issues. Limit to at most 64MB of colormap (this is overkill)
        if (chunk.TotalChunks <= 0 || chunk.TotalChunks > 1024) {
            Logger.Warn($"Invalid TotalChunks {chunk.TotalChunks} from {player.PlayerName}, ignoring chunk");
            return;
        }

        ChunkedTransfer transfer = _activeTransfers.GetOrAdd(chunk.TransferId, _ => new ChunkedTransfer {
            PlayerId = player.PlayerUID,
            PlayerName = player.PlayerName,
            TotalChunks = chunk.TotalChunks,
            ReceivedChunks = new byte[chunk.TotalChunks][],
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        // Validate transfer belongs to this player
        if (transfer.PlayerId != player.PlayerUID) {
            Logger.Warn($"Player {player.PlayerName} tried to send chunk for transfer owned by another player");
            return;
        }

        // Validate TotalChunks consistency to prevent malformed transfers
        if (chunk.TotalChunks != transfer.TotalChunks) {
            Logger.Warn($"Chunk TotalChunks mismatch from {player.PlayerName}: expected {transfer.TotalChunks}, got {chunk.TotalChunks}, invalidating transfer");
            _activeTransfers.TryRemove(chunk.TransferId, out _);
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.error".ToLang(), EnumChatType.CommandError);
            return;
        }

        // Store the chunk
        if (chunk.ChunkIndex >= 0 && chunk.ChunkIndex < transfer.TotalChunks) {
            // Validate chunk size to prevent memory abuse (max 64KB + small buffer)
            if (chunk.Data.Length > 70000) {
                Logger.Warn($"Chunk {chunk.ChunkIndex} from {player.PlayerName} exceeds max size ({chunk.Data.Length} bytes), invalidating transfer");
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

            Logger.Debug($"Received colormap chunk {chunk.ChunkIndex + 1}/{chunk.TotalChunks} from {player.PlayerName}");

            // Check if transfer is complete
            if (isComplete) {
                CompleteTransfer(player, chunk.TransferId, transfer);
            }
        } else {
            // Invalid chunk index - log warning and invalidate the entire transfer
            Logger.Warn($"Invalid chunk index {chunk.ChunkIndex} (expected 0-{transfer.TotalChunks - 1}) from {player.PlayerName}, invalidating transfer");
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
            ColormapPacket packet = new() { RawBase64String = base64 };

            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.received".ToLang(), EnumChatType.CommandSuccess);
            Logger.Info($"Colormap packet was received from {player.PlayerName} ({transfer.TotalChunks} chunks)");

            _server.Colormap.LoadFromPacket(_server.Sapi.World, packet);
        } catch (Exception e) {
            Logger.Error($"Failed to reassemble colormap from {player.PlayerName}: {e}");
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
                Logger.Warn($"Colormap transfer from {transfer.PlayerName} timed out ({transfer.ChunksReceived}/{transfer.TotalChunks} chunks received)");
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
        public int ChunksReceived { get; set; }
        public bool IsCompleted { get; set; }
    }
}
