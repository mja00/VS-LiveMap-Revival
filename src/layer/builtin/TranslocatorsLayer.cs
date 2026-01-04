using System.Collections.Concurrent;
using livemap.configuration;
using livemap.data;
using livemap.layer.marker;
using livemap.layer.marker.options;
using livemap.util;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace livemap.layer.builtin;

public class TranslocatorsLayer : Layer {
    private readonly string _knownFile;

    private readonly ConcurrentDictionary<ulong, HashSet<Translocator>> _knownTranslocators;

    private bool _dirty;

    public TranslocatorsLayer() : base("translocators", "lang.translocators".ToLang()) {
        _knownFile = Path.Combine(Files.JsonDir, $"{Id}.json");

        ConcurrentDictionary<ulong, HashSet<Translocator>>? translocators = null;
        if (File.Exists(_knownFile)) {
            try {
                string json = File.ReadAllText(_knownFile);
                translocators = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, HashSet<Translocator>>>(json);
            } catch (Exception e) {
                Logger.Warn($"Failed to load translocators from '{_knownFile}': {e.Message}");
            }
        }

        _knownTranslocators = translocators ?? new ConcurrentDictionary<ulong, HashSet<Translocator>>();
    }
    public override int? Interval => Config.UpdateInterval;

    public override bool? Hidden => !Config.DefaultShowLayer;

    public override List<Marker> Markers {
        get {
            List<Marker> list = [];
            Point spawnPos = LiveMap.Api.Sapi.World.DefaultSpawnPosition.ToPoint();
            _knownTranslocators.Values.Foreach(translocators => translocators.Foreach(translocator => {
                TooltipOptions? tooltip = Config.Tooltip?.DeepCopy();
                if (tooltip?.Content != null) {
                    // Convert to relative coordinates (relative to spawn) for display
                    Point relPos = translocator.Pos.ToPoint().Subtract(spawnPos);
                    Point relTarget = translocator.TargetLocation.ToPoint().Subtract(spawnPos);
                    string positionStr = $"{(int)relPos.X}, {(int)relPos.Z}";
                    string targetStr = "lang.tooltip.translocator".ToLang($"{(int)relTarget.X}, {(int)relTarget.Z}");
                    tooltip.Content = string.Format(tooltip.Content, positionStr, targetStr);
                }

                PopupOptions? popup = Config.Popup?.DeepCopy();
                if (popup?.Content != null) {
                    // Convert to relative coordinates (relative to spawn) for display
                    Point relPos = translocator.Pos.ToPoint().Subtract(spawnPos);
                    Point relTarget = translocator.TargetLocation.ToPoint().Subtract(spawnPos);
                    string positionStr = $"{(int)relPos.X}, {(int)relPos.Z}";
                    string targetStr = $"{(int)relTarget.X}, {(int)relTarget.Z}";
                    popup.Content = "lang.popup.translocator".ToLang(positionStr, targetStr);
                }

                string id = $"translocator:{translocator.Pos.X},{translocator.Pos.Y},{translocator.Pos.Z}";
                list.Add(new Icon(id, translocator.Pos.ToPoint(), Config.IconOptions) { Tooltip = tooltip, Popup = popup });
            }));
            return list;
        }
    }

    public override string? Css => Config.Css;

    public override string Filename => Path.Combine(Files.MarkerDir, $"{Id}.json");

    private static Translocators Config => LiveMap.Api.Config.Layers.Translocators;

    public void SetTranslocators(ulong chunkIndex, HashSet<Translocator> translocator) {
        if (translocator.Count == 0) {
            _knownTranslocators.Remove(chunkIndex);
        } else {
            _knownTranslocators[chunkIndex] = translocator;
        }

        _dirty = true;
    }

    public override async Task WriteToDisk(CancellationToken cancellationToken) {
        if (Config.Enabled) {
            if (_dirty) {
                string knownJson = JsonConvert.SerializeObject(_knownTranslocators, Files.JsonSerializerMinifiedSettings);

                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                await Files.WriteJsonAsync(_knownFile, knownJson, cancellationToken);
                _dirty = false;

                if (cancellationToken.IsCancellationRequested) {
                    return;
                }
            }

            await base.WriteToDisk(cancellationToken);
        } else {
            // Translocators disabled - delete the JSON file if it exists
            if (File.Exists(Filename)) {
                try {
                    File.Delete(Filename);
                } catch (Exception e) {
                    Logger.Warn($"Failed to delete Translocators layer file '{Filename}': {e.Message}");
                }
            }
        }
    }

    public class Translocator(BlockPos pos, BlockPos targetLocation) {
        public readonly BlockPos Pos = pos;
        public readonly BlockPos TargetLocation = targetLocation;
    }
}
