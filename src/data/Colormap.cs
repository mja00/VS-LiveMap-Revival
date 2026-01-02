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

    public void Add(string block, uint[] toAdd) {
        _colorsByName.TryAdd(block, toAdd);
    }

    public bool TryGet(int id, [MaybeNullWhen(false)] out uint[] colors) {
        return _colorsById.TryGetValue(id, out colors);
    }

    public int Count => _colorsById.Count;

    public string Serialize() {
        return JsonConvert.SerializeObject(_colorsByName);
    }

    public bool Deserialize(string? json) {
        _colorsByName.Clear();

        if (string.IsNullOrEmpty(json)) {
            return false;
        }

        try {
            Dictionary<string, uint[]> data = JsonConvert.DeserializeObject<Dictionary<string, uint[]>>(json)!;
            foreach ((string? key, uint[]? colors) in data) {
                _colorsByName.TryAdd(key, colors);
            }

            return true;
        } catch (Exception e) {
            Logger.Error(e.ToString());
            return false;
        }
    }

    public void LoadFromPacket(IWorldAccessor world, ColormapPacket packet) {
        new Thread(_ => {
            if (Deserialize(packet.Decompress().RawColormap)) {
                SaveToDisk();
                RefreshIds(world);
                Logger.Info("colormap.saved-to-disk".ToLang());
            } else {
                Logger.Warn("colormap.could-not-save-to-disk".ToLang());
            }
        }).Start();
    }

    public void LoadFromDisk(IWorldAccessor world) {
        new Thread(_ => {
            string? json = null;
            if (File.Exists(Files.ColormapFile)) {
                json = File.ReadAllText(Files.ColormapFile, Encoding.UTF8);
            }

            if (Deserialize(json)) {
                RefreshIds(world);
                Logger.Info("colormap.loaded-from-disk".ToLang());
            } else {
                Logger.Warn("colormap.could-not-load-from-disk".ToLang());
            }
        }).Start();
    }

    public void SaveToDisk() {
        File.WriteAllText(Files.ColormapFile, Serialize(), Encoding.UTF8);
    }

    public void RefreshIds(IWorldAccessor world) {
        _colorsById.Clear();

        foreach ((string code, uint[] colors) in _colorsByName) {
            Block block = world.GetBlock(new AssetLocation(code));
            if (block == null) {
                Logger.Warn("colormap.invalid-block-id".ToLang(code));
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

    public void Dispose() {
        _colorsByName.Clear();
    }
}
