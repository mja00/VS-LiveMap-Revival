using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using livemap.data;
using livemap.network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace livemap;

[HarmonyPatch]
public sealed class LiveMapClient {
    private static BlockPos? _overridePos;

    private readonly LiveMapMod _mod;
    private readonly ICoreClientAPI _api;
    private readonly ILogger _logger;
    private readonly Harmony _harmony;

    private IClientNetworkChannel? _channel;

    public LiveMapClient(LiveMapMod mod, ICoreClientAPI api) {
        _mod = mod;
        _api = api;
        _logger = mod.Mod.Logger;

        _channel = api.Network.RegisterChannel(mod.Mod.Info.ModID)
            .RegisterMessageType<ColormapPacket>()
            .SetMessageHandler<ColormapPacket>(_ => {
                _logger.Event("Received colormap request from server");

                if (!api.World.Player.HasPrivilege(Privilege.root)) {
                    _logger.Event("No privilege to use this mod");
                    return;
                }

                Colormap? colormap = GenerateColormap();

                if (colormap == null || _channel is not { Connected: true }) {
                    return;
                }

                _logger.Event("Sending generated colormap to server");
                string json = colormap.Serialize();

                FileInfo fileInfo = new(Path.Combine(GamePaths.ModConfig, "colormap.json"));
                try {
                    File.WriteAllText(fileInfo.FullName, json);
                    _logger.Event($"Wrote colormap to disk.");
                } catch (Exception e) {
                    _logger.Event($"Error sending colormap to server: {e}");
                }

                // todo - rework this so it doesnt send one giant packet
                _channel.SendPacket(new ColormapPacket { RawColormap = json }.Compress());
            });

        _harmony = new Harmony(mod.Mod.Info.ModID);
        _harmony.PatchAll();
    }

    private Colormap? GenerateColormap() {
        if (_overridePos != null) {
            return null;
        }

        Colormap colormap = new();
        EntityPlayer player = _api.World.Player.Entity;
        _overridePos = player.SidedPos.AsBlockPos;

        try {
            foreach (Block block in player.World.Blocks.Where(block => block.Code != null)) {
                if (_overridePos == null) {
                    return null;
                }

                uint baseColor = Color.Reverse((uint)block.GetColor(_api, _overridePos));
                uint[] colors = new uint[30];
                for (int i = 0; i < colors.Length; i++) {
                    uint randColor = (uint)block.GetRandomColor(_api, _overridePos, BlockFacing.UP, i);
                    uint color = Color.Blend(baseColor, randColor, 0.4F);
                    colors[i] = color & 0xFFFFFF;
                }

                colormap.Add(block.Code.ToString(), colors);
            }
        } catch (Exception e) {
            _logger.Error(e.ToString());
        }

        _overridePos = null;

        return colormap;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameCalendar), "get_YearRel")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static bool PreYearRel(IGameCalendar __instance, ref float __result) {
        if (_overridePos == null) {
            return true;
        }

        __result = __instance.GetHemisphere(_overridePos) == EnumHemisphere.North ? 0.6f : 0.1f;
        return false;
    }

    public void Dispose() {
        _channel = null;
        _overridePos = null;
        _harmony.UnpatchAll(_mod.Mod.Info.ModID);
    }
}
