using livemap.render;
using livemap.util;
using Newtonsoft.Json;

namespace livemap.task;

public sealed class SettingsTask(LiveMap server) : AsyncTask(server) {
    private const int _interval = 30;

    private long _lastUpdate;

    protected override async Task TickAsync(CancellationToken cancellationToken) {
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (now - _lastUpdate < _interval) {
            return;
        }

        _lastUpdate = now;

        Dictionary<string, object?> dict = [];
        dict.TryAdd("friendlyUrls", _server.Config.Web.FriendlyUrls);
        dict.TryAdd("playerList", _server.Config.Layers.Players.Enabled);
        dict.TryAdd("playerMarkers", _server.Config.Layers.Players.Enabled);
        dict.TryAdd("maxPlayers", _server.Sapi.Server.Config.MaxClients);
        dict.TryAdd("interval", _interval);
        dict.TryAdd("size", _server.Sapi.WorldManager.Size());
        dict.TryAdd("spawn", _server.Sapi.World.DefaultSpawnPosition.ToPoint());
        dict.TryAdd("web", new Dictionary<string, object?> { { "tiletype", _server.Config.Web.TileType.Type } });
        dict.TryAdd("zoom", new Dictionary<string, object?> { { "def", _server.Config.Zoom.Default }, { "maxin", _server.Config.Zoom.MaxIn }, { "maxout", _server.Config.Zoom.MaxOut } });
        dict.TryAdd("renderers", Renderers(cancellationToken));
        dict.TryAdd("ui", new Dictionary<string, object?> {
            { "attribution", _server.Config.Ui.Attribution },
            { "logolink", _server.Config.Ui.LogoLink },
            { "logoimg", _server.Config.Ui.LogoImg },
            { "logotext", _server.Config.Ui.LogoText },
            { "sitetitle", _server.Config.Ui.SiteTitle },
            { "sidebar", _server.Config.Ui.Sidebar }
        });
        dict.TryAdd("lang", new Dictionary<string, object?> {
            { "pinned", "lang.pinned".ToLang() },
            { "unpinned", "lang.unpinned".ToLang() },
            { "players", "lang.players".ToLang() },
            { "avatar", "lang.avatar".ToLang() },
            { "avatar-alt", "lang.avatar-alt".ToLang() },
            { "renderers", "lang.renderers".ToLang() },
            { "copy", "lang.copy".ToLang() },
            { "copy-alt", "lang.copy-alt".ToLang() },
            { "paste", "lang.paste".ToLang() },
            { "paste-alt", "lang.paste-alt".ToLang() },
            { "share", "lang.share".ToLang() },
            { "share-alt", "lang.share-alt".ToLang() },
            { "center", "lang.center".ToLang() },
            { "center-alt", "lang.center-alt".ToLang() },
            { "notif-copy", "lang.notif-copy".ToLang() },
            { "notif-copy-failed", "lang.notif-copy-failed".ToLang() },
            { "notif-paste", "lang.notif-paste".ToLang() },
            { "notif-paste-failed", "lang.notif-paste-failed".ToLang() },
            { "notif-paste-invalid", "lang.notif-paste-invalid".ToLang() },
            { "notif-share", "lang.notif-share".ToLang() },
            { "notif-share-failed", "lang.notif-share-failed".ToLang() },
            { "notif-center", "lang.notif-center".ToLang() },
            { "share-title", "lang.share-title".ToLang() },
            { "renderer.basic", "renderer.basic".ToLang() },
            { "renderer.sepia", "renderer.sepia".ToLang() },
            { "spawn", "lang.spawn".ToLang() },
            { "zoom-in", "lang.zoom-in".ToLang() },
            { "zoom-out", "lang.zoom-out".ToLang() }
        });

        try {
            string json = JsonConvert.SerializeObject(dict);

            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            await Files.WriteJsonAsync(Path.Combine(Files.JsonDir, "settings.json"), json, cancellationToken);
        } catch (Exception e) {
            await Console.Error.WriteLineAsync(e.ToString());
        }
    }

    private Dictionary<string, string>[] Renderers(CancellationToken cancellationToken) {
        List<Dictionary<string, string>> dict = [];
        List<Renderer> renderers = new(_server.RendererRegistry.Values);
        foreach (Renderer renderer in renderers) {
            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            Dictionary<string, string> obj = [];
            obj.TryAdd("id", renderer.Id);
            obj.TryAdd("icon", "" /*renderer.Icon*/); // todo
            dict.AddIfNotExists(obj);
        }

        return [.. dict];
    }
}
