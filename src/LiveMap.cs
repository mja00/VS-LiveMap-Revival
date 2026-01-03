using livemap.command;
using livemap.configuration;
using livemap.data;
using livemap.httpd;
using livemap.network;
using livemap.registry;
using livemap.task;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace livemap;

public sealed class LiveMap {
    private readonly EventCoordinator _eventCoordinator;
    private readonly NetworkHandler _networkHandler;
    private readonly LiveMapMod _mod;

    public LiveMap(LiveMapMod mod, ICoreServerAPI api) {
        Api = this;

        Sapi = api;
        _mod = mod;

        Files.SavegameIdentifier = Sapi.World.SavegameIdentifier;
        GamePaths.EnsurePathExists(GamePaths.ModConfig);
        GamePaths.EnsurePathExists(Files.DataDir);

        ConfigManager = new ConfigManager(this);
        Reload();

        Files.ExtractWebFiles(this);

        Colormap = new Colormap();
        SepiaColors = new SepiaColors(this);

        CommandHandler = new CommandHandler(this);

        LayerRegistry = [];
        RendererRegistry = [];

        AsyncTaskManager = new AsyncTaskManager(this);
        RenderTaskManager = new RenderTaskManager(this);
        WebServer = new WebServer(this);

        _eventCoordinator = new EventCoordinator(this);
        _networkHandler = new NetworkHandler(this);

        // things to do on first game tick
        Sapi.Event.RegisterCallback(_ => {
            Colormap.LoadFromDisk(Sapi.World);
        }, 1);
    }

    public static LiveMap Api { get; private set; } = null!;

    public ICoreServerAPI Sapi { get; }

    public string ModId => _mod.Mod.Info.ModID;

    public ConfigManager ConfigManager { get; }

    public Config Config => ConfigManager.Config;

    public Colormap Colormap { get; }
    public SepiaColors SepiaColors { get; }

    public CommandHandler CommandHandler { get; }

    public LayerRegistry LayerRegistry { get; }
    public RendererRegistry RendererRegistry { get; }

    public AsyncTaskManager? AsyncTaskManager { get; private set; }
    public RenderTaskManager? RenderTaskManager { get; private set; }

    public WebServer? WebServer { get; }

    public void Reload() {
        AsyncTaskManager?.Dispose();
        AsyncTaskManager = null;

        RenderTaskManager?.Dispose();
        RenderTaskManager = null;

        ConfigManager.Reload();

        WebServer?.Reload();

        AsyncTaskManager = new AsyncTaskManager(this);
        RenderTaskManager = new RenderTaskManager(this);
    }

    public void SendPacket<T>(T packet, IPlayer? receiver = null) => _networkHandler.SendPacket(packet, receiver);

    public void Dispose() {
        _eventCoordinator.Dispose();
        _networkHandler.Dispose();
        ConfigManager.Dispose();

        CommandHandler.Dispose();

        AsyncTaskManager?.Dispose();
        AsyncTaskManager = null;

        RenderTaskManager?.Dispose();
        RenderTaskManager = null;

        LayerRegistry.Dispose();
        RendererRegistry.Dispose();

        Colormap.Dispose();
        SepiaColors.Dispose();

        WebServer?.Dispose();
    }
}
