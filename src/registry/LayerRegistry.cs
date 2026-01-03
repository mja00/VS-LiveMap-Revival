using livemap.layer;
using livemap.layer.builtin;

namespace livemap.registry;

public class LayerRegistry() : Registry<Layer>("layers") {
    public PlayersLayer? Players { get; private set; }
    public SpawnLayer? Spawn { get; private set; }
    public TradersLayer? Traders { get; private set; }
    public TranslocatorsLayer? Translocators { get; private set; }
    public VSCartographerLayer? VSCartographer { get; private set; }

    public void RegisterBuiltIns() {
        Register(Players = new PlayersLayer());
        Register(Spawn = new SpawnLayer());
        Register(Traders = new TradersLayer());
        Register(Translocators = new TranslocatorsLayer());
        Register(VSCartographer = new VSCartographerLayer());
    }

    public override void Dispose() {
        Players = null;
        Spawn = null;
        Traders = null;
        Translocators = null;
        VSCartographer = null;

        base.Dispose();
    }
}
