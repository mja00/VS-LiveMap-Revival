using livemap.command;
using livemap.configuration;
using livemap.data;
using livemap.httpd;
using livemap.network;
using livemap.registry;
using livemap.task;
using livemap.util;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace livemap;

public sealed class LiveMap {
    public static LiveMap Api { get; private set; } = null!;

    public ICoreServerAPI Sapi { get; }

    public string ModId => _mod.Mod.Info.ModID;

    public Config Config { get; private set; } = null!;

    public Colormap Colormap { get; }
    public SepiaColors SepiaColors { get; }

    public CommandHandler CommandHandler { get; }

    public LayerRegistry LayerRegistry { get; }
    public RendererRegistry RendererRegistry { get; }

    public AsyncTaskManager? AsyncTaskManager { get; private set; }
    public RenderTaskManager? RenderTaskManager { get; private set; }

    public WebServer? WebServer { get; }

    private readonly LiveMapMod _mod;
    private readonly FileWatcher _configFileWatcher;
    private readonly long _gameTickTaskId;
    private readonly ColormapReceiver _colormapReceiver;
    private int _lastMonth = -1;

    private IServerNetworkChannel? _channel;

    public LiveMap(LiveMapMod mod, ICoreServerAPI api) {
        Api = this;

        Sapi = api;
        _mod = mod;

        Files.SavegameIdentifier = Sapi.World.SavegameIdentifier;
        GamePaths.EnsurePathExists(GamePaths.ModConfig);
        GamePaths.EnsurePathExists(Files.DataDir);

        _configFileWatcher = new FileWatcher(this);
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

        api.Event.ChunkDirty += OnChunkDirty;
        api.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        api.Event.GameWorldSave += OnGameWorldSave;

        // things to do on first game tick
        Sapi.Event.RegisterCallback(_ => {
            RendererRegistry.RegisterBuiltIns();
            LayerRegistry.RegisterBuiltIns();
            CheckSeason();
        }, 1);

        _gameTickTaskId = Sapi.Event.RegisterGameTickListener(OnGameTick, 1000, 1000);

        _colormapReceiver = new ColormapReceiver(this);

        _channel = Sapi.Network.RegisterChannel(ModId)
            .RegisterMessageType<ColormapPacket>()
            .RegisterMessageType<ColormapChunkPacket>()
            .SetMessageHandler<ColormapPacket>(ReceiveColormap)
            .SetMessageHandler<ColormapChunkPacket>(_colormapReceiver.ReceiveChunk);
    }

    public void Reload() {
        AsyncTaskManager?.Dispose();
        AsyncTaskManager = null;

        RenderTaskManager?.Dispose();
        RenderTaskManager = null;

        LoadConfig();
        SaveConfig();

        WebServer?.Reload();

        AsyncTaskManager = new AsyncTaskManager(this);
        RenderTaskManager = new RenderTaskManager(this);
    }

    public void LoadConfig() {
        Config = Sapi.LoadModConfig<Config>($"{ModId}.json") ?? new Config();
    }

    public void SaveConfig() {
        _configFileWatcher.IgnoreChanges = true;

        FileInfo fileInfo = new(Path.Combine(GamePaths.ModConfig, $"{ModId}.json"));
        GamePaths.EnsurePathExists(fileInfo.Directory!.FullName);
        string json = JsonConvert.SerializeObject(Config, Files.JsonSerializerPrettySettings);
        File.WriteAllText(fileInfo.FullName, json);

        Sapi.Event.RegisterCallback(_ => _configFileWatcher.IgnoreChanges = false, 100);
    }

    public void SendPacket<T>(T packet, IPlayer? receiver = null) {
        _channel?.SendPacket(packet, receiver as IServerPlayer);
    }

    private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason) {
        // queue it up, it will process when the game saves
        Logger.Debug("chunk.dirty".ToLang(chunkCoord));
        RenderTaskManager?.Queue(chunkCoord.X >> 4, chunkCoord.Z >> 4);
    }

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks) {
        Logger.Debug("chunk.loaded".ToLang(chunkCoord));
        RenderTaskManager?.Queue(chunkCoord.X >> 4, chunkCoord.Y >> 4);
    }

    private void OnGameWorldSave() {
        // delay a bit to ensure chunks actually save to disk first
        Sapi.Event.RegisterCallback(_ => RenderTaskManager?.ProcessQueue(), 1000);
    }

    // this method ticks every 1000ms on the game thread
    private void OnGameTick(float delta) {
        // ensure render task is running
        //RenderTask.Run();

        // ensure web server is still running
        WebServer?.Run();

        // todo - update player positions, public waypoints, etc
        AsyncTaskManager?.Tick();

        CheckSeason();
    }

    private void CheckSeason() {
        int currentMonth = Sapi.World.Calendar.Month;
        if (currentMonth == _lastMonth) {
            return;
        }

        // Only do full render if it's not the first load (lastMonth != -1)
        bool shouldRender = _lastMonth != -1 && Config.Render.FullRenderOnSeasonChange;

        _lastMonth = currentMonth;
        Logger.Info($"Season changed to month {currentMonth}. Loading seasonal colormap...");

        Colormap.LoadFromDisk(Sapi.World, currentMonth);

        if (shouldRender) {
            Logger.Info("Triggering full map render due to season change...");
            RenderTaskManager?.QueueAll();
        }
    }

    private void ReceiveColormap(IServerPlayer player, ColormapPacket packet) {
        if (!player.HasPrivilege(Privilege.root)) {
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.error.no-privilege".ToLang(), EnumChatType.CommandError);
            Logger.Warn("colormap.non-privileged".ToLang(player.PlayerName));
            return;
        }

        if (string.IsNullOrEmpty(packet.RawBase64String)) {
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.empty".ToLang(), EnumChatType.CommandError);
            Logger.Warn("colormap.empty".ToLang(player.PlayerName));
            return;
        }

        player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.received".ToLang(), EnumChatType.CommandSuccess);
        Logger.Info("colormap.received".ToLang(player.PlayerName));
        Colormap.LoadFromPacket(Sapi.World, packet);
    }

    public void Dispose() {
        _configFileWatcher.Dispose();

        Sapi.Event.ChunkDirty -= OnChunkDirty;
        Sapi.Event.ChunkColumnLoaded -= OnChunkColumnLoaded;
        Sapi.Event.GameWorldSave -= OnGameWorldSave;

        Sapi.Event.UnregisterGameTickListener(_gameTickTaskId);

        CommandHandler.Dispose();

        AsyncTaskManager?.Dispose();
        AsyncTaskManager = null;

        RenderTaskManager?.Dispose();
        RenderTaskManager = null;

        LayerRegistry.Dispose();
        RendererRegistry.Dispose();

        Colormap.Dispose();
        SepiaColors.Dispose();

        _colormapReceiver.Dispose();

        WebServer?.Dispose();

        _channel = null;
    }
}
