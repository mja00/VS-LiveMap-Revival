using System.Collections.Concurrent;
using livemap.configuration;
using livemap.layer.marker;
using livemap.layer.marker.options;
using livemap.util;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace livemap.layer.builtin;

public class TradersLayer : Layer {
    public override int? Interval => Config.UpdateInterval;

    public override bool? Hidden => !Config.DefaultShowLayer;

    public override List<Marker> Markers {
        get {
            List<Marker> list = [];
            _knownTraders.Values.Foreach(traders => {
                traders.Foreach(trader => {
                    TooltipOptions? tooltip = Config.Tooltip?.DeepCopy();
                    if (tooltip?.Content != null) {
                        tooltip.Content = string.Format(tooltip.Content, trader.Name, trader.Type);
                    }

                    PopupOptions? popup = Config.Popup?.DeepCopy();
                    if (popup?.Content != null) {
                        popup.Content = string.Format(popup.Content, trader.Name, trader.Type);
                    }

                    list.Add(new Icon($"trader:{trader.Id}", trader.Pos.ToPoint(), Config.IconOptions) {
                        Tooltip = tooltip,
                        Popup = popup
                    });
                });
            });
            return list;
        }
    }

    public override string? Css => Config.Css;

    public override string Filename => Path.Combine(Files.MarkerDir, $"{Id}.json");

    private static Traders Config => LiveMap.Api.Config.Layers.Traders;

    private readonly ConcurrentDictionary<ulong, HashSet<Trader>> _knownTraders;
    private readonly string _knownFile;

    private bool _dirty;

    public TradersLayer() : base("traders", "lang.traders".ToLang()) {
        _knownFile = Path.Combine(Files.JsonDir, $"{Id}.json");

        ConcurrentDictionary<ulong, HashSet<Trader>>? traders = null;
        if (File.Exists(_knownFile)) {
            try {
                string json = File.ReadAllText(_knownFile);
                traders = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, HashSet<Trader>>>(json);
            } catch (Exception) {
                // ignored
            }
        }

        _knownTraders = traders ?? new ConcurrentDictionary<ulong, HashSet<Trader>>();
    }

    public void SetTraders(ulong chunkIndex, HashSet<Trader> traders) {
        if (traders.Count == 0) {
            _knownTraders.Remove(chunkIndex);
        } else {
            _knownTraders[chunkIndex] = traders;
        }

        _dirty = true;
    }

    public override async Task WriteToDisk(CancellationToken cancellationToken) {
        if (_dirty) {
            string knownJson = JsonConvert.SerializeObject(_knownTraders, Files.JsonSerializerMinifiedSettings);

            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            await Files.WriteJsonAsync(_knownFile, knownJson, cancellationToken);

            if (cancellationToken.IsCancellationRequested) {
                return;
            }
        }

        await base.WriteToDisk(cancellationToken);
    }

    public class Trader(string type, long id, string name, Vec3i pos) {
        public readonly string Type = type;
        public readonly long Id = id;
        public readonly string Name = name;
        public readonly Vec3i Pos = pos;
    }
}
