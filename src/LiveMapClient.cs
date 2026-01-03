using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using livemap.data;
using livemap.network;
using livemap.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace livemap;

[HarmonyPatch]
public sealed class LiveMapClient {
    [ThreadStatic] private static BlockPos? _overridePos;
    [ThreadStatic] private static float? _overrideMonth;

    private readonly LiveMapMod _mod;
    private readonly ICoreClientAPI _api;
    private readonly ILogger _logger;
    private readonly Harmony _harmony;

    private IClientNetworkChannel? _channel;

    // Lock object for thread-safe patching
    private static readonly object _patchLock = new();
    private bool _patched;

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

                EnsurePatched();

                new Thread(() => {
                    EntityPlayer player = _api.World.Player.Entity;
                    _overridePos = player.SidedPos.AsBlockPos;
                    try {
                        for (int month = 1; month <= 12; month++) {
                            // Calculate YearRel for the middle of each month (approximate)
                            // 12 months = 1.0 YearRel
                            // Month 1 (Jan) ~= 0.0 - 0.08
                            // Middle of Month 1 ~= 0.04
                            // Formula: (month - 0.5) / 12.0
                            _overrideMonth = (month - 0.5f) / 12.0f;
                            int currentMonth = month; // Fix access to modified closure

                            _logger.Event($"Generating colormap for month {month}...");
                            api.Event.EnqueueMainThreadTask(() => api.ShowChatMessage($"Generating colormap for month {currentMonth}/12..."), "livemap-chat");

                            if (_channel is not { Connected: true }) {
                                _logger.Warning("[LiveMap] Connection lost during colormap generation. Aborting.");
                                return;
                            }

                            Colormap? colormap = GenerateColormap();
                            if (colormap == null) {
                                _logger.Warning($"[LiveMap] Failed to generate colormap for month {month}. Skipping.");
                                continue;
                            }

                            string json = colormap.Serialize();
                            ColormapPacket responsePacket = new ColormapPacket { RawColormap = json, Month = month }.Compress();
                            ColormapChunkPacket[] chunks = responsePacket.ToChunks().ToArray();

                            for (int i = 0; i < chunks.Length; i++) {
                                _channel.SendPacket(chunks[i]);
                                Thread.Sleep(10); // Throttle slightly
                            }

                            _logger.Event($"Sent colormap for month {month}");
                        }

                        api.Event.EnqueueMainThreadTask(() => api.ShowChatMessage("command.colormap.sent".ToLang(12)), "livemap-chat");
                    } finally {
                        _overridePos = null;
                        _overrideMonth = null;
                    }
                }).Start();
            });

        _harmony = new Harmony(mod.Mod.Info.ModID);
    }

    private void EnsurePatched() {
        if (_patched) {
            return;
        }

        lock (_patchLock) {
            if (_patched) {
                return;
            }

            try {
                // Target the base GameCalendar class directly as it contains the logic we want to override
                Type calendarType = typeof(GameCalendar);
                _logger.Event($"[LiveMap] Patching calendar base type: {calendarType.FullName}");

                MethodInfo? yearRelGetter = AccessTools.PropertyGetter(calendarType, "YearRel");
                if (yearRelGetter != null) {
                    _harmony.Patch((MethodBase)yearRelGetter, prefix: new HarmonyMethod(GetType(), nameof(PreYearRel)));
                    _logger.Event("[LiveMap] Patched YearRel successfully");
                } else {
                    _logger.Warning("[LiveMap] Could not find YearRel getter on GameCalendar");
                }


                _patched = true;
            } catch (Exception e) {
                _logger.Error($"[LiveMap] Failed to patch calendar: {e}");
            }
        }
    }

    private Colormap? GenerateColormap() {
        if (_overridePos == null) {
            return null;
        }


        Colormap colormap = new();
        EntityPlayer player = _api.World.Player.Entity;

        try {
            foreach (Block block in player.World.Blocks.Where(block => block.Code != null)) {
                uint baseColor;
                if (block is BlockRequireSolidGround) {
                    baseColor = Color.Reverse((uint)_api.BlockTextureAtlas.GetAverageColor(block.TextureSubIdForBlockColor));
                } else if (block is BlockPlant) {
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

        return colormap;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static bool PreYearRel(IGameCalendar __instance, ref float __result) {
        if (_overrideMonth == null) {
            return true;
        }

        __result = _overrideMonth.Value;
        return false;
    }


    public void Dispose() {
        _channel = null;
        _harmony.UnpatchAll(_mod.Mod.Info.ModID);
    }
}
