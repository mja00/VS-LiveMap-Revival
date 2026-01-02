using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using livemap.data;
using livemap.network;
using livemap.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

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
            .RegisterMessageType<ColormapChunkPacket>()
            .SetMessageHandler<ColormapPacket>(_ => {
                _logger.Event("colormap.request-received".ToLang());

                if (!api.World.Player.HasPrivilege(Privilege.root)) {
                    _logger.Event("no.privilege".ToLang());
                    return;
                }

                Colormap? colormap = GenerateColormap();

                if (colormap == null || _channel is not { Connected: true }) {
                    return;
                }

                _logger.Event("colormap.sending-generated".ToLang());
                api.ShowChatMessage("command.colormap.generating".ToLang());
                string json = colormap.Serialize();

                FileInfo fileInfo = new(Path.Combine(GamePaths.ModConfig, "colormap.json"));
                try {
                    File.WriteAllText(fileInfo.FullName, json);
                    _logger.Event("colormap.wrote".ToLang());
                } catch (Exception e) {
                    _logger.Event("colormap.error-saving".ToLang(e));
                }

                // Send colormap in chunks to avoid exceeding packet size limit
                ColormapPacket packet = new ColormapPacket { RawColormap = json }.Compress();
                ColormapChunkPacket[] chunks = packet.ToChunks().ToArray();
                _logger.Event("colormap.sending".ToLang(chunks.Length));

                // Show progress at milestones to avoid spamming chat
                int lastMilestone = 0;
                for (int i = 0; i < chunks.Length; i++) {
                    _channel.SendPacket(chunks[i]);

                    // Show progress at 25%, 50%, 75%, 100% milestones
                    int percent = (i + 1) * 100 / chunks.Length;
                    int milestone = percent / 25 * 25; // Round down to nearest 25
                    if (milestone > lastMilestone || i == chunks.Length - 1) {
                        api.ShowChatMessage("command.colormap.sending".ToLang(i + 1, chunks.Length));
                        lastMilestone = milestone;
                    }
                }

                api.ShowChatMessage("command.colormap.sent".ToLang(chunks.Length));
                _logger.Event("colormap.sent".ToLang(chunks.Length));
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

                uint baseColor;
                if (block is BlockPlant) {
                    Block tallGrassBlock = _api.World.GetBlock(new AssetLocation("game:tallgrass-tall-free"));
                    baseColor = Color.Reverse((uint)tallGrassBlock.GetColor(_api, _overridePos));
                } else {
                    baseColor = Color.Reverse((uint)block.GetColor(_api, _overridePos));
                }

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

    public void Dispose() {
        _channel = null;
        _overridePos = null;
        _harmony.UnpatchAll(_mod.Mod.Info.ModID);
    }
}
