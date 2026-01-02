using Vintagestory.API.Config;

namespace livemap.util;

public class FileWatcher {
    private readonly FileSystemWatcher _watcher;
    private readonly LiveMap _server;

    public bool IgnoreChanges { get; set; }

    private bool _queued;

    public FileWatcher(LiveMap server) {
        _server = server;

        _watcher = new FileSystemWatcher(GamePaths.ModConfig) { Filter = $"{server.ModId}.json", IncludeSubdirectories = false, EnableRaisingEvents = true };

        _watcher.Changed += Changed;
        _watcher.Created += Changed;
        _watcher.Deleted += Changed;
        _watcher.Renamed += Changed;
        _watcher.Error += Error;
    }

    private void Changed(object sender, FileSystemEventArgs e) {
        QueueReload(true);
    }

    private void Error(object sender, ErrorEventArgs e) {
        Logger.Error(e.GetException().ToString());
        QueueReload();
    }

    /// <summary>
    /// My workaround for <a href='https://github.com/dotnet/runtime/issues/24079'>dotnet#24079</a>.
    /// </summary>
    private void QueueReload(bool changed = false) {
        // check if already queued for reload
        if (IgnoreChanges || _queued) {
            return;
        }

        // mark as queued
        _queued = true;

        // inform console/log
        if (changed) {
            Logger.Info("config.changed".ToLang());
        }

        // wait for other changes to process
        _server.Sapi.Event.RegisterCallback(_ => {
            // reload the config
            _server.Reload();

            // wait some more to remove this change from the queue since the reload triggers another write
            _server.Sapi.Event.RegisterCallback(_ => {
                // unmark as queued
                _queued = false;
            }, 100);
        }, 100);
    }

    public void Dispose() {
        _watcher.Changed -= Changed;
        _watcher.Created -= Changed;
        _watcher.Deleted -= Changed;
        _watcher.Renamed -= Changed;
        _watcher.Error -= Error;

        _watcher.Dispose();
    }
}
