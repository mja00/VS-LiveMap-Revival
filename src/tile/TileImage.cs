using livemap.configuration;
using livemap.util;
using SkiaSharp;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace livemap.tile;

public unsafe class TileImage {
    private readonly SKBitmap _bitmap;
    private readonly byte* _bitmapPtr;

    private readonly int _bitmapRowBytes;

    private readonly int _regionX;
    private readonly int _regionZ;
    private readonly byte[] _shadowMap;

    public TileImage(int regionX, int regionZ) {
        _bitmap = new SKBitmap(TileConstants.RegionSize, TileConstants.RegionSize);
        _bitmapPtr = (byte*)_bitmap.GetPixels().ToPointer();
        _shadowMap = new byte[TileConstants.RegionBlockCount].Fill((byte)128);

        _bitmapRowBytes = _bitmap.RowBytes;

        _regionX = regionX;
        _regionZ = regionZ;
    }

    public void SetBlockColor(int blockX, int blockZ, uint argb, float yDiff) {
        int imgX = blockX & TileConstants.RegionMask;
        int imgZ = blockZ & TileConstants.RegionMask;

        ((uint*)(_bitmapPtr + (imgZ * _bitmapRowBytes)))[imgX] = argb;

        _shadowMap[(imgZ << TileConstants.RegionSizeBitShift) + imgX] = (byte)(_shadowMap[(imgZ << TileConstants.RegionSizeBitShift) + imgX] * yDiff);
    }

    public void CalculateShadows() {
        byte[] shadowMapCopy = [.. _shadowMap];
        BlurTool.Blur(_shadowMap, TileConstants.RegionSize, TileConstants.RegionSize, 2);
        for (int i = 0; i < _shadowMap.Length; i++) {
            float shadow = (int)(((_shadowMap[i] / 128F) - 1F) * 5F) / 5F;
            shadow += ((shadowMapCopy[i] / 128F) - 1F) * 5F % 1F / 5F;

            int imgX = i & TileConstants.RegionMask;
            int imgZ = i >> TileConstants.RegionSizeBitShift;

            uint* row = (uint*)(_bitmapPtr + (imgZ * _bitmapRowBytes));
            row[imgX] = (uint)(row[imgX] == 0 ? 0 : ColorUtil.ColorMultiply3Clamped((int)row[imgX], (shadow * 1.4F) + 1F));
        }
    }

    public void Save(string rendererId) {
        try {
            Config config = LiveMap.Api.Config;
            for (int zoom = 0; zoom <= config.Zoom.MaxOut; zoom++) {
                FileInfo fileInfo = new(Path.Combine(Files.TilesDir, rendererId, zoom.ToString(), $"{_regionX >> zoom}_{_regionZ >> zoom}.{config.Web.TileType.Type}"));
                GamePaths.EnsurePathExists(fileInfo.Directory!.FullName);

                if (zoom > 0) {
                    SKBitmap bitmap;
                    using (FileStream inStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) {
                        bitmap = SKBitmap.Decode(inStream) ?? new SKBitmap(TileConstants.RegionSize, TileConstants.RegionSize);
                    }

                    WritePixels(bitmap, zoom);

                    using (FileStream outStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) {
                        bitmap.Encode(config.Web.TileType.Format, config.Web.TileQuality).SaveTo(outStream);
                    }
                    bitmap.Dispose();
                } else {
                    using FileStream outStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    _bitmap.Encode(config.Web.TileType.Format, config.Web.TileQuality).SaveTo(outStream);
                }
            }
        } catch (Exception e) {
            Logger.Error(e.ToString());
        }
    }

    private void WritePixels(SKBitmap png, int zoom) {
        int step = 1 << zoom;
        int baseX = ((_regionX * TileConstants.RegionSize) >> zoom) & TileConstants.RegionMask;
        int baseZ = ((_regionZ * TileConstants.RegionSize) >> zoom) & TileConstants.RegionMask;
        byte* pngPtr = (byte*)png.GetPixels().ToPointer();
        int pngRowBytes = png.RowBytes;
        for (int x = 0; x < TileConstants.RegionSize; x += step) {
            for (int z = 0; z < TileConstants.RegionSize; z += step) {
                uint argb = ((uint*)(_bitmapPtr + (z * _bitmapRowBytes)))[x];
                if (argb == 0) {
                    // skipping 0 prevents overwrite existing
                    // parts of the buffer of existing images
                    continue;
                }

                if (step > 1) {
                    // merge pixel colors instead of skipping them
                    argb = DownSample(x, z, argb, step);
                }

                ((uint*)(pngPtr + ((baseZ + (z >> zoom)) * pngRowBytes)))[baseX + (x >> zoom)] = argb;
            }
        }
    }

    private uint DownSample(int x, int z, uint argb, int step) {
        uint a = 0, r = 0, g = 0, b = 0, c = 0;
        for (int i = 0; i < step; i++) {
            for (int j = 0; j < step; j++) {
                if (i != 0 && j != 0) {
                    argb = ((uint*)(_bitmapPtr + ((z + j) * _bitmapRowBytes)))[x + i];
                }

                a += (argb >> 24) & 0xFF;
                r += (argb >> 16) & 0xFF;
                g += (argb >> 8) & 0xFF;
                b += (argb >> 0) & 0xFF;
                c++;
            }
        }

        return c == 0 ? 0 : ((a / c) << 24) | ((r / c) << 16) | ((g / c) << 8) | (b / c);
    }

    public void Dispose() => _bitmap.Dispose();
}
