using System.IO.Compression;
using System.Text;
using ProtoBuf;

namespace livemap.network;

[ProtoContract]
public sealed class ColormapPacket : Packet {
    public string? RawColormap;

    [ProtoMember(1)] public string? RawBase64String;

    public ColormapPacket Compress() {
        byte[] originalBytes = Encoding.UTF8.GetBytes(RawColormap ?? "");

        using MemoryStream compressedStream = new();
        using (GZipStream gzip = new(compressedStream, CompressionMode.Compress)) {
            gzip.Write(originalBytes, 0, originalBytes.Length);
            gzip.Close();
        }

        byte[] compressedBytes = compressedStream.ToArray();

        RawBase64String = Convert.ToBase64String(compressedBytes);

        return this;
    }

    public ColormapPacket Decompress() {
        byte[] compressedBytes = Convert.FromBase64String(RawBase64String ?? "");

        using MemoryStream compressedStream = new(compressedBytes);
        using MemoryStream decompressedStream = new();
        using (GZipStream gzip = new(compressedStream, CompressionMode.Decompress)) {
            gzip.CopyTo(decompressedStream);
            gzip.Close();
        }

        byte[] decompressedBytes = decompressedStream.ToArray();
        RawColormap = Encoding.UTF8.GetString(decompressedBytes);

        return this;
    }

    /// <summary>
    /// Splits the compressed colormap data into smaller chunks for transfer.
    /// </summary>
    /// <param name="maxChunkSize">Maximum size of each chunk in bytes. Default is 64KB.</param>
    /// <returns>An enumerable of ColormapChunkPacket instances.</returns>
    public IEnumerable<ColormapChunkPacket> ToChunks(int maxChunkSize = 65536) {
        if (string.IsNullOrEmpty(RawBase64String)) {
            yield break;
        }

        byte[] compressedBytes = Convert.FromBase64String(RawBase64String);

        // Reject empty data - prevents edge case where TotalChunks would be 0
        if (compressedBytes.Length == 0) {
            yield break;
        }

        string transferId = Guid.NewGuid().ToString();
        int totalChunks = (int)Math.Ceiling((double)compressedBytes.Length / maxChunkSize);

        for (int i = 0; i < totalChunks; i++) {
            int offset = i * maxChunkSize;
            int length = Math.Min(maxChunkSize, compressedBytes.Length - offset);
            byte[] chunkData = new byte[length];
            Array.Copy(compressedBytes, offset, chunkData, 0, length);

            yield return new ColormapChunkPacket {
                TransferId = transferId,
                ChunkIndex = i,
                TotalChunks = totalChunks,
                Data = chunkData
            };
        }
    }
}
