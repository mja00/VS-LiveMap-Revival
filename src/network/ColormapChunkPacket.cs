using ProtoBuf;

namespace livemap.network;

/// <summary>
/// A single chunk of colormap data for chunked transfer.
/// Used to avoid exceeding Vintage Story's packet size limit.
/// </summary>
[ProtoContract]
public sealed class ColormapChunkPacket : Packet {
    /// <summary>
    /// Unique identifier for this transfer session.
    /// All chunks with the same TransferId belong together.
    /// </summary>
    [ProtoMember(1)]
    public string TransferId { get; set; } = "";

    /// <summary>
    /// Zero-based index of this chunk.
    /// </summary>
    [ProtoMember(2)]
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total number of chunks in this transfer.
    /// </summary>
    [ProtoMember(3)]
    public int TotalChunks { get; set; }

    /// <summary>
    /// The chunk data (portion of the compressed colormap).
    /// </summary>
    [ProtoMember(4)]
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// The month this colormap belongs to (1-12).
    /// </summary>
    [ProtoMember(5)]
    public int Month { get; set; }
}
