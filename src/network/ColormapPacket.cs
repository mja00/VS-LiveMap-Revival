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
}
