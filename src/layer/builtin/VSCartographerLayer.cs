using System.Collections;
using System.Net;
using System.Reflection;
using livemap.configuration;
using livemap.data;
using livemap.layer.marker;
using livemap.layer.marker.options;
using livemap.layer.marker.options.type;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace livemap.layer.builtin;

public class VSCartographerLayer : Layer {
    private bool _isModInstalled;
    private object? _sharedLayer;
    private PropertyInfo? _waypointsProperty;
    private FieldInfo? _waypointsField;

    public VSCartographerLayer() : base("vscartographer", "lang.vscartographer".ToLang()) {
        _isModInstalled = DetectVSCartographer();
    }

    public override int? Interval => Config.UpdateInterval;

    public override bool? Hidden {
        get {
            return !Config.DefaultShowLayer || !_isModInstalled;
        }
    }

    public override List<Marker> Markers {
        get {
            if (!_isModInstalled || _sharedLayer == null || (_waypointsProperty == null && _waypointsField == null)) {
                return [];
            }

            try {
                object? waypointsDict = _waypointsProperty != null
                    ? _waypointsProperty.GetValue(_sharedLayer)
                    : _waypointsField?.GetValue(_sharedLayer);
                if (waypointsDict == null) {
                    return [];
                }

                List<Marker> markers = [];

                // waypointsDict is Dictionary<string, List<SharedWaypoint>>
                // We need to iterate through it using reflection
                if (waypointsDict is IDictionary dict) {
                    foreach (DictionaryEntry entry in dict) {
                        if (entry.Value is IEnumerable waypointList) {
                            List<object?> waypointArray = waypointList.Cast<object?>().ToList();
                            foreach (object? waypoint in waypointArray) {
                                if (waypoint == null) {
                                    continue;
                                }

                                Marker? marker = ConvertWaypointToMarker(waypoint);
                                if (marker != null) {
                                    markers.Add(marker);
                                }
                            }
                        }
                    }
                }

                return markers;
            } catch (Exception e) {
                Logger.Warn($"Failed to access VSCartographer waypoints: {e.Message}");
                return [];
            }
        }
    }

    public override string Filename => Path.Combine(Files.MarkerDir, $"{Id}.json");

    private static VSCartographer Config => LiveMap.Api.Config.Layers.VSCartographer;

    private bool DetectVSCartographer() {
        try {
            // Check if mod is installed
            List<Mod> allMods = LiveMap.Api.Sapi.ModLoader.Mods.ToList();
            Mod? mod = allMods.FirstOrDefault(m => m.Info.ModID == "nbcartographer");

            if (mod == null) {
                return false;
            }

            // Get WorldMapManager
            WorldMapManager? worldMapManager = LiveMap.Api.Sapi.ModLoader.GetModSystem<WorldMapManager>();
            if (worldMapManager == null) {
                return false;
            }

            // Find SharedWaypointMapLayer using reflection
            // The layer is registered with layer group "sharedwaypoints"
            MemberInfo? mapLayersMember = FindMember(
                typeof(WorldMapManager),
                ["MapLayers", "mapLayers", "_mapLayers"],
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (mapLayersMember == null) {
                return false;
            }

            object? mapLayers = GetMemberValue(mapLayersMember, worldMapManager);
            if (mapLayers == null || mapLayers is not IEnumerable layers) {
                return false;
            }

            // Find the SharedWaypointMapLayer by checking layer group code
            List<object?> layerList = layers.Cast<object?>().ToList();
            foreach (object? layer in layerList) {
                if (layer == null) {
                    continue;
                }

                // Check if this layer has LayerGroupCode property equal to "sharedwaypoints"
                PropertyInfo? layerGroupCodeProperty = layer.GetType().GetProperty("LayerGroupCode", BindingFlags.Public | BindingFlags.Instance);
                if (layerGroupCodeProperty != null) {
                    object? layerGroupCode = layerGroupCodeProperty.GetValue(layer);
                    if (layerGroupCode?.ToString() == "sharedwaypoints") {
                        _sharedLayer = layer;
                        // Try to find Waypoints as property or field
                        MemberInfo? waypointsMember = FindPropertyOrField(
                            layer.GetType(),
                            "Waypoints",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        );

                        if (waypointsMember != null) {
                            if (waypointsMember is PropertyInfo property) {
                                _waypointsProperty = property;
                            } else if (waypointsMember is FieldInfo field) {
                                _waypointsField = field;
                            }
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        } catch (Exception e) {
            Logger.Warn($"Failed to detect VSCartographer: {e.Message}");
            return false;
        }
    }

    private static Icon? ConvertWaypointToMarker(object waypoint) {
        try {
            // Access waypoint properties via reflection
            Type waypointType = waypoint.GetType();

            // Get Position - it's a public field, not a property
            MemberInfo? positionMember = FindPropertyOrField(waypointType, "Position", BindingFlags.Public | BindingFlags.Instance);
            if (positionMember == null) {
                return null;
            }

            object? position = GetMemberValue(positionMember, waypoint);
            if (position == null) {
                return null;
            }

            // Extract X and Z from Vec3d - check both fields and properties
            Type positionType = position.GetType();
            MemberInfo? xMember = FindPropertyOrField(positionType, "X", BindingFlags.Public | BindingFlags.Instance);
            MemberInfo? zMember = FindPropertyOrField(positionType, "Z", BindingFlags.Public | BindingFlags.Instance);
            if (xMember == null || zMember == null) {
                return null;
            }

            double x = Convert.ToDouble(GetMemberValue(xMember, position) ?? 0);
            double z = Convert.ToDouble(GetMemberValue(zMember, position) ?? 0);
            Point point = new Point(x, z);

            // Get Title - check both field and property
            MemberInfo? titleMember = FindPropertyOrField(waypointType, "Title", BindingFlags.Public | BindingFlags.Instance);
            string title = GetMemberValue(titleMember, waypoint)?.ToString() ?? "Unknown";

            // Get Guid for unique ID - check both field and property
            MemberInfo? guidMember = FindPropertyOrField(waypointType, "Guid", BindingFlags.Public | BindingFlags.Instance);
            string guid = GetMemberValue(guidMember, waypoint)?.ToString() ?? Guid.NewGuid().ToString();

            // Get OwningPlayerUid for tooltip - check both field and property
            MemberInfo? owningPlayerUidMember = FindPropertyOrField(waypointType, "OwningPlayerUid", BindingFlags.Public | BindingFlags.Instance);
            string? owningPlayerUid = GetMemberValue(owningPlayerUidMember, waypoint)?.ToString();

            // Convert player UID to player name
            string? owningPlayer = null;
            if (!string.IsNullOrEmpty(owningPlayerUid)) {
                try {
                    // First, try to find online player
                    Vintagestory.API.Server.IServerPlayer? onlinePlayer = LiveMap.Api.Sapi.World.AllOnlinePlayers
                        .Cast<Vintagestory.API.Server.IServerPlayer>()
                        .FirstOrDefault(p => p.PlayerUID == owningPlayerUid);

                    if (onlinePlayer != null) {
                        owningPlayer = onlinePlayer.PlayerName;
                    } else {
                        // Player not online, try to get from PlayerData
                        Vintagestory.API.Server.IServerPlayerData playerData = LiveMap.Api.Sapi.PlayerData.GetPlayerDataByUid(owningPlayerUid);
                        if (playerData != null) {
                            // Try LastKnownPlayername as a field or property
                            MemberInfo? nameMember = FindPropertyOrField(
                                playerData.GetType(),
                                "LastKnownPlayername",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                            );
                            if (nameMember != null) {
                                owningPlayer = GetMemberValue(nameMember, playerData)?.ToString();
                            }
                        }
                    }
                } catch {
                    // Silently fail - just won't show player name
                }
            }

            // HTML encode user-controlled content to prevent XSS
            string safeTitle = WebUtility.HtmlEncode(title);
            string safeOwningPlayer = WebUtility.HtmlEncode(owningPlayer ?? "");

            // Create icon options
            IconOptions iconOptions = Config.IconOptions.DeepCopy();

            // Create tooltip
            TooltipOptions? tooltip = Config.Tooltip?.DeepCopy();
            if (tooltip?.Content != null) {
                string tooltipText = !string.IsNullOrEmpty(owningPlayer) ? $"{safeTitle} (by {safeOwningPlayer})" : safeTitle;
                tooltip.Content = string.Format(tooltip.Content, tooltipText);
            }

            // Create popup
            PopupOptions? popup = Config.Popup?.DeepCopy();
            if (popup?.Content != null) {
                popup.Content = !string.IsNullOrEmpty(owningPlayer) ? $"{safeTitle}<br>Created by: {safeOwningPlayer}" : safeTitle;
            }

            Icon icon = new Icon($"vscartographer:{guid}", point, iconOptions) {
                Tooltip = tooltip,
                Popup = popup
            };

            return icon;
        } catch (Exception e) {
            Logger.Warn($"Failed to convert VSCartographer waypoint to marker: {e.Message}");
            return null;
        }
    }

    public override async Task WriteToDisk(CancellationToken cancellationToken) {
        if (Config.Enabled && _isModInstalled) {
            await base.WriteToDisk(cancellationToken);
        }
    }

    // Helper methods for reflection
    private static MemberInfo? FindMember(Type type, string[] names, BindingFlags flags) {
        foreach (string name in names) {
            // Try property first
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property != null) {
                return property;
            }

            // Then try field
            FieldInfo? field = type.GetField(name, flags);
            if (field != null) {
                return field;
            }
        }
        return null;
    }

    private static object? GetMemberValue(MemberInfo? member, object? instance) {
        return member == null || instance == null
            ? null
            : member switch {
            PropertyInfo property => property.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => null
        };
    }

    private static MemberInfo? FindPropertyOrField(Type type, string name, BindingFlags flags) {
        // Try property first
        PropertyInfo? property = type.GetProperty(name, flags);
        if (property != null) {
            return property;
        }

        // Then try field
        FieldInfo? field = type.GetField(name, flags);
        return field;
    }
}
