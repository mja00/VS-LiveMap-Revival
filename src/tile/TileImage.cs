using System.Runtime.CompilerServices;
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
    private readonly Config _config;
    private volatile bool _hasChanges;
    private readonly HashSet<int> _changedZoomLevels = [];
    private readonly object _changeTrackingLock = new();

    public TileImage(int regionX, int regionZ) {
        _bitmap = new SKBitmap(TileConstants.RegionSize, TileConstants.RegionSize);
        _bitmapPtr = (byte*)_bitmap.GetPixels().ToPointer();
        _shadowMap = new byte[TileConstants.RegionBlockCount].Fill((byte)128);

        _bitmapRowBytes = _bitmap.RowBytes;

        _regionX = regionX;
        _regionZ = regionZ;
        _config = LiveMap.Api.Config;
        _hasChanges = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlockColor(int blockX, int blockZ, uint argb, float yDiff) {
        int imgX = blockX & TileConstants.RegionMask;
        int imgZ = blockZ & TileConstants.RegionMask;

        ((uint*)(_bitmapPtr + (imgZ * _bitmapRowBytes)))[imgX] = argb;

        // Set shadow value based on base value (128) and yDiff multiplier
        // yDiff is a multiplier factor from ProcessShadow (typically 0.92-1.58 range)
        int shadowIndex = (imgZ << TileConstants.RegionSizeBitShift) + imgX;
        // Ensure we're clamped to a valid range
        _shadowMap[shadowIndex] = (byte)Math.Clamp(_shadowMap[shadowIndex] * yDiff, 0, 255);

        // Thread-safe change tracking: only update if not already marked as changed
        if (!_hasChanges) {
            lock (_changeTrackingLock) {
                // Double-check pattern: verify still unchanged after acquiring lock
                if (!_hasChanges) {
                    _hasChanges = true;
                    // When base tile (zoom 0) changes, all zoom levels need updating
                    // Mark all zoom levels as changed for incremental save tracking
                    for (int zoom = 0; zoom <= _config.Zoom.MaxOut; zoom++) {
                        _changedZoomLevels.Add(zoom);
                    }
                }
            }
        }
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
            bool incrementalSaves = _config.Render.EnableIncrementalSaves;

            // Thread-safe check: capture current state atomically
            bool hasChanges;
            HashSet<int> changedZoomLevels;
            lock (_changeTrackingLock) {
                hasChanges = _hasChanges;
                // Create a copy of changed zoom levels to avoid holding lock during I/O
                changedZoomLevels = [.. _changedZoomLevels];
            }

            // Skip entirely if no changes and incremental saves enabled
            // If _hasChanges is false, _changedZoomLevels should be empty (SetBlockColor sets both together)
            if (incrementalSaves && !hasChanges) {
                return;
            }

            // Higher zoom levels depend on lower ones (downsampling from base bitmap)
            // If any zoom level changes, all higher zoom levels must also be updated
            // Find the minimum changed zoom level to determine the update range
            int minChangedZoom = incrementalSaves && changedZoomLevels.Count > 0
                ? changedZoomLevels.Min()
                : 0;

            for (int zoom = 0; zoom <= _config.Zoom.MaxOut; zoom++) {
                // Skip unchanged zoom levels when incremental saves enabled
                // Update if this zoom level is >= the minimum changed zoom level
                if (incrementalSaves && zoom > 0 && zoom < minChangedZoom) {
                    continue;
                }

                FileInfo fileInfo = new(Path.Combine(Files.TilesDir, rendererId, zoom.ToString(), $"{_regionX >> zoom}_{_regionZ >> zoom}.{_config.Web.TileType.Type}"));
                GamePaths.EnsurePathExists(fileInfo.Directory!.FullName);

                if (zoom > 0) {
                    SKBitmap bitmap;
                    // Optimize: Use FileMode.Open if file exists, faster than OpenOrCreate
                    // Handle TOCTOU race condition: file may be deleted between Exists check and Open
                    if (fileInfo.Exists) {
                        try {
                            using FileStream inStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            bitmap = SKBitmap.Decode(inStream) ?? new SKBitmap(TileConstants.RegionSize, TileConstants.RegionSize);
                        } catch (FileNotFoundException) {
                            // File was deleted between Exists check and Open (TOCTOU race condition)
                            bitmap = new SKBitmap(TileConstants.RegionSize, TileConstants.RegionSize);
                        }
                    } else {
                        bitmap = new SKBitmap(TileConstants.RegionSize, TileConstants.RegionSize);
                    }

                    WritePixels(bitmap, zoom);

                    using FileStream outStream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    bitmap.Encode(_config.Web.TileType.Format, _config.Web.TileQuality).SaveTo(outStream);

                    bitmap.Dispose();
                } else {
                    // Zoom level 0 always saves (base tile)
                    using FileStream outStream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    _bitmap.Encode(_config.Web.TileType.Format, _config.Web.TileQuality).SaveTo(outStream);
                }
            }

        } catch (Exception e) {
            Logger.Error(e.ToString());
        } finally {
            // Always ensure change tracking is cleared
            lock (_changeTrackingLock) {
                _hasChanges = false;
                _changedZoomLevels.Clear();
            }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
