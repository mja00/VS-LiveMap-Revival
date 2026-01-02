using Xunit;
using System.IO.Compression;
using System.Text;

namespace LiveMap.Tests;

/// <summary>
/// Tests for the chunking logic used in colormap packet transfer.
/// These tests verify the core chunking algorithm without requiring Vintage Story dependencies.
/// </summary>
public class ColormapChunkingTest {
    /// <summary>
    /// Simulates the ToChunks algorithm for testing purposes, avoiding VS dependencies.
    /// </summary>
    private static List<byte[]> SplitIntoChunks(byte[] data, int maxChunkSize) {
        var chunks = new List<byte[]>();
        int totalChunks = (int)Math.Ceiling((double)data.Length / maxChunkSize);

        for (int i = 0; i < totalChunks; i++) {
            int offset = i * maxChunkSize;
            int length = Math.Min(maxChunkSize, data.Length - offset);
            byte[] chunkData = new byte[length];
            Array.Copy(data, offset, chunkData, 0, length);
            chunks.Add(chunkData);
        }

        return chunks;
    }

    /// <summary>
    /// Reassembles chunks back into original data.
    /// </summary>
    private static byte[] ReassembleChunks(List<byte[]> chunks) {
        int totalLength = chunks.Sum(c => c.Length);
        byte[] result = new byte[totalLength];
        int offset = 0;

        foreach (var chunk in chunks) {
            Array.Copy(chunk, 0, result, offset, chunk.Length);
            offset += chunk.Length;
        }

        return result;
    }

    [Fact]
    public void SplitIntoChunks_EmptyData_ReturnsNoChunks() {
        // Edge case: Empty data should yield no chunks.
        // This prevents TotalChunks=0 which would cause the receiver
        // to immediately "complete" with empty data.
        // The client validates this and won't send empty colormaps.
        byte[] data = [];
        var chunks = SplitIntoChunks(data, 1024);

        Assert.Empty(chunks);
    }

    [Fact]
    public void SplitIntoChunks_SmallData_ReturnsSingleChunk() {
        byte[] data = Encoding.UTF8.GetBytes("small test data");
        var chunks = SplitIntoChunks(data, 1024);

        Assert.Single(chunks);
        Assert.Equal(data, chunks[0]);
    }

    [Fact]
    public void SplitIntoChunks_LargeData_ReturnsMultipleChunks() {
        // 1000 bytes with 100 byte chunks should give 10 chunks
        byte[] data = new byte[1000];
        new Random(42).NextBytes(data);

        var chunks = SplitIntoChunks(data, 100);

        Assert.Equal(10, chunks.Count);
        Assert.All(chunks, c => Assert.Equal(100, c.Length));
    }

    [Fact]
    public void SplitIntoChunks_UnevenSize_LastChunkIsSmaller() {
        // 250 bytes with 100 byte chunks: 3 chunks (100, 100, 50)
        byte[] data = new byte[250];
        new Random(42).NextBytes(data);

        var chunks = SplitIntoChunks(data, 100);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(100, chunks[0].Length);
        Assert.Equal(100, chunks[1].Length);
        Assert.Equal(50, chunks[2].Length);
    }

    [Fact]
    public void ChunksCanBeReassembled() {
        // Create random data
        byte[] original = new byte[5000];
        new Random(42).NextBytes(original);

        // Split and reassemble
        var chunks = SplitIntoChunks(original, 512);
        byte[] reassembled = ReassembleChunks(chunks);

        Assert.Equal(original, reassembled);
    }

    [Fact]
    public void GzipCompressedData_CanBeChunkedAndReassembled() {
        // Simulate the actual colormap workflow: compress -> chunk -> reassemble -> decompress
        string originalJson = "{\"block1\":[1,2,3,4],\"block2\":[5,6,7,8]}";
        byte[] originalBytes = Encoding.UTF8.GetBytes(originalJson);

        // Compress
        using var compressedStream = new MemoryStream();
        using (var gzip = new GZipStream(compressedStream, CompressionMode.Compress)) {
            gzip.Write(originalBytes, 0, originalBytes.Length);
        }

        byte[] compressedData = compressedStream.ToArray();

        // Chunk
        var chunks = SplitIntoChunks(compressedData, 16);

        // Reassemble
        byte[] reassembled = ReassembleChunks(chunks);
        Assert.Equal(compressedData, reassembled);

        // Decompress
        using var decompressStream = new MemoryStream(reassembled);
        using var decompressedStream = new MemoryStream();
        using (var gzip = new GZipStream(decompressStream, CompressionMode.Decompress)) {
            gzip.CopyTo(decompressedStream);
        }

        string result = Encoding.UTF8.GetString(decompressedStream.ToArray());

        Assert.Equal(originalJson, result);
    }

    [Fact]
    public void LargeColormap_ChunkCount_IsReasonable() {
        // Simulate a large colormap (1MB of data)
        byte[] largeData = new byte[1024 * 1024];
        new Random(42).NextBytes(largeData);

        // With 64KB chunks, should be ~16 chunks
        var chunks = SplitIntoChunks(largeData, 65536);

        Assert.Equal(16, chunks.Count);
    }
}
