using System.Diagnostics.CodeAnalysis;
using System.Text;
using livemap.network;
using livemap.util;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace livemap.data;

public sealed class Colormap {
    private readonly Dictionary<string, uint[]> _colorsByName = [];
    private readonly Dictionary<int, uint[]> _colorsById = [];
    private readonly object _lock = new();

    public void Add(string block, uint[] toAdd) {
        lock (_lock) {
            _colorsByName.TryAdd(block, toAdd);
        }
    }

    public bool TryGet(int id, [MaybeNullWhen(false)] out uint[] colors) {
        lock (_lock) {
            return _colorsById.TryGetValue(id, out colors);
        }
    }

    public int Count {
        get {
            lock (_lock) {
                return _colorsById.Count;
            }
        }
    }

    public string Serialize() {
        lock (_lock) {
            return JsonConvert.SerializeObject(_colorsByName);
        }
    }

    public bool Deserialize(string? json) {
        lock (_lock) {
            _colorsByName.Clear();

            if (string.IsNullOrEmpty(json)) {
                return false;
            }

            try {
                Dictionary<string, uint[]> data = JsonConvert.DeserializeObject<Dictionary<string, uint[]>>(json)!;
                foreach ((string key, uint[] colors) in data) {
                    _colorsByName.TryAdd(key, colors);
                }

                return true;
            } catch (Exception e) {
                Logger.Error(e.ToString());
                return false;
            }
        }
    }

    public void LoadFromPacket(IWorldAccessor world, ColormapPacket packet) {
        new Thread(_ => {
            if (Deserialize(packet.Decompress().RawColormap)) {
                SaveToDisk(packet.Month);
                RefreshIds(world);
                Logger.Info($"Colormap for month {packet.Month} saved to disk");
            } else {
                Logger.Warn("colormap.could-not-save-to-disk".ToLang());
            }
        }).Start();
    }

    public void LoadFromDisk(IWorldAccessor world, int month = -1) {
        new Thread(_ => {
            string? json = null;
            string path = month > 0 ? Files.GetColormapFile(month) : Files.ColormapFile;

            // If specific month file is missing, try to migrate or fall back
            if (month > 0 && !File.Exists(path)) {
                bool migrated = false;

                // Try to migrate from legacy/default file if it exists
                if (File.Exists(Files.ColormapFile)) {
                    try {
                        File.Copy(Files.ColormapFile, path);
                        Logger.Info($"Migrated default colormap to {Path.GetFileName(path)}");
                        migrated = true;
                    } catch (Exception e) {
                        Logger.Error($"Failed to migrate colormap: {e.Message}");
                    }
                }

                // If migration didn't happen (failed or no source), fall back to default
                if (!migrated) {
                    Logger.Warn($"Seasonal colormap {path} not found, falling back to default.");
                    path = Files.ColormapFile;
                }
            }

            if (File.Exists(path)) {
                json = File.ReadAllText(path, Encoding.UTF8);
            }

            if (Deserialize(json)) {
                RefreshIds(world);
                Logger.Info($"Colormap loaded from disk ({Path.GetFileName(path)})");
            } else {
                Logger.Warn("colormap.could-not-load-from-disk".ToLang());
            }
        }).Start();
    }

    public void SaveToDisk(int month = -1) {
        string path = month > 0 ? Files.GetColormapFile(month) : Files.ColormapFile;
        File.WriteAllText(path, Serialize(), Encoding.UTF8);
    }

    public void RefreshIds(IWorldAccessor world) {
        lock (_lock) {
            _colorsById.Clear();

            foreach ((string code, uint[] colors) in _colorsByName) {
                Block block = world.GetBlock(new AssetLocation(code));
                if (block == null) {
                    Logger.Warn($"Invalid block id in colormap ({code})");
                    continue;
                }

                // add opaque alpha channel back
                for (int i = 0; i < colors.Length; i++) {
                    if (colors[i] > 0) {
                        colors[i] |= (uint)0xFF << 24;
                    }
                }

                _colorsById.TryAdd(block.Id, colors);
            }
        }
    }

    public void Dispose() {
        lock (_lock) {
            _colorsByName.Clear();
            _colorsById.Clear();
        }
    }
}
