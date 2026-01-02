using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace livemap;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class LiveMapMod : ModSystem {
    private LiveMapClient? _client;
    private LiveMap? _server;

    public override void StartClientSide(ICoreClientAPI api) {
        _client = new LiveMapClient(this, api);
    }

    public override void StartServerSide(ICoreServerAPI api) {
        _server = new LiveMap(this, api);
    }

    public override void Dispose() {
        _client?.Dispose();
        _client = null;

        _server?.Dispose();
        _server = null;
    }
}
