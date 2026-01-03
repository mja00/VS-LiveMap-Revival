using livemap.util;
using Newtonsoft.Json;
using Vintagestory.API.Config;

namespace livemap.configuration;

public sealed class ConfigManager : IDisposable {
    private readonly FileWatcher _fileWatcher;
    private readonly livemap.LiveMap _server;

    public ConfigManager(livemap.LiveMap server) {
        _server = server;
        _fileWatcher = new FileWatcher(server);
        Load();
    }

    public Config Config { get; private set; } = null!;

    public void Load() => Config = _server.Sapi.LoadModConfig<Config>($"{_server.ModId}.json") ?? new Config();

    public void Save() {
        _fileWatcher.IgnoreChanges = true;

        FileInfo fileInfo = new(Path.Combine(GamePaths.ModConfig, $"{_server.ModId}.json"));
        GamePaths.EnsurePathExists(fileInfo.Directory!.FullName);
        string json = JsonConvert.SerializeObject(Config, Files.JsonSerializerPrettySettings);
        File.WriteAllText(fileInfo.FullName, json);

        _server.Sapi.Event.RegisterCallback(_ => _fileWatcher.IgnoreChanges = false, 100);
    }

    public void Reload() {
        Load();
        Save();
    }

    public void Dispose() {
        _fileWatcher.Dispose();
    }
}
