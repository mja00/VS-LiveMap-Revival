using livemap.configuration;
using livemap.data;
using livemap.layer.marker;
using livemap.util;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace livemap.layer.builtin;

public class PlayersLayer() : Layer("players", "lang.players".ToLang()) {
    public override int? Interval => Config.UpdateInterval;

    public override bool? Hidden => !Config.DefaultShowLayer;

    public override List<Marker> Markers { get; } = [];

    public override string Filename => Path.Combine(Files.JsonDir, "players.json");

    public override bool Private => true;

    private static Players Config => LiveMap.Api.Config.Layers.Players;

    public override async Task WriteToDisk(CancellationToken cancellationToken) {
        List<Dictionary<string, object?>> players = [];

        if (Config.Enabled) {
            List<IServerPlayer> onlinePlayers = new(LiveMap.Api.Sapi.World.AllOnlinePlayers.Cast<IServerPlayer>());
            foreach (IServerPlayer player in onlinePlayers) {
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                ProcessPlayer(player, players);
            }
        }

        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        string json = JsonConvert.SerializeObject(new Dictionary<string, object?> { { "interval", Config.UpdateInterval }, { "hidden", !Config.DefaultShowLayer }, { "players", players } }, Files.JsonSerializerMinifiedSettings);

        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        await Files.WriteJsonAsync(Filename, json, cancellationToken);
    }

    private static void ProcessPlayer(IServerPlayer player, List<Dictionary<string, object?>> players) {
        EntityPlayer entity = player.Entity;
        if (entity == null) {
            return;
        }

        if (Config.HideSpectators && player.WorldData.CurrentGameMode == EnumGameMode.Spectator) {
            return;
        }

        if (Config.HideIfSneaking && entity.Controls.Sneak) {
            return;
        }

        if (Config.HideUnderBlocks && entity.Pos.Y < entity.World.BlockAccessor.GetRainMapHeightAt(entity.Pos.AsBlockPos)) {
            return;
        }

        Color? color = null;
        if (player.Entitlements?.Count > 0 && GlobalConstants.playerColorByEntitlement.TryGetValue(player.Entitlements[0].Code, out double[]? arr)) {
            color = new Color(arr);
        }

        Dictionary<string, object?> dict = [];
        dict.TryAdd("id", player.PlayerUID);
        dict.TryAdd("name", player.PlayerName);
        dict.TryAdd("avatar", entity.GetAvatar());
        dict.TryAdd("role", player.Role.Code);
        dict.TryAdd("color", color?.ToString(false));
        dict.TryAdd("pos", player.GetPoint());
        dict.TryAdd("yaw", 180 - ((entity.Pos?.Yaw ?? 0) * (180.0 / Math.PI)));
        dict.TryAdd("health", player.GetHealth());
        dict.TryAdd("satiety", player.GetSatiety());

        players.Add(dict);
    }
}
