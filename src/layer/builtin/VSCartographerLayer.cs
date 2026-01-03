using System.Collections;
using System.Reflection;
using livemap.configuration;
using livemap.data;
using livemap.layer.marker;
using livemap.layer.marker.options;
using livemap.layer.marker.options.type;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
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
                            var waypointArray = waypointList.Cast<object?>().ToList();
                            foreach (object? waypoint in waypointArray) {
                                if (waypoint == null) continue;

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
            var allMods = LiveMap.Api.Sapi.ModLoader.Mods.ToList();
            var mod = allMods.FirstOrDefault(m => m.Info.ModID == "nbcartographer");

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
            // Try public property first, then private/protected, then field
            PropertyInfo? mapLayersProperty = typeof(WorldMapManager).GetProperty("MapLayers", BindingFlags.Public | BindingFlags.Instance)
                ?? typeof(WorldMapManager).GetProperty("MapLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            
            FieldInfo? mapLayersField = null;
            if (mapLayersProperty == null) {
                mapLayersField = typeof(WorldMapManager).GetField("MapLayers", BindingFlags.Public | BindingFlags.Instance)
                    ?? typeof(WorldMapManager).GetField("MapLayers", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(WorldMapManager).GetField("mapLayers", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(WorldMapManager).GetField("_mapLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            if (mapLayersProperty == null && mapLayersField == null) {
                return false;
            }

            object? mapLayers = mapLayersProperty != null 
                ? mapLayersProperty.GetValue(worldMapManager)
                : mapLayersField?.GetValue(worldMapManager);
            if (mapLayers == null || mapLayers is not IEnumerable layers) {
                return false;
            }

            // Find the SharedWaypointMapLayer by checking layer group code
            var layerList = layers.Cast<object?>().ToList();
            foreach (object? layer in layerList) {
                if (layer == null) continue;

                // Check if this layer has LayerGroupCode property equal to "sharedwaypoints"
                PropertyInfo? layerGroupCodeProperty = layer.GetType().GetProperty("LayerGroupCode", BindingFlags.Public | BindingFlags.Instance);
                if (layerGroupCodeProperty != null) {
                    object? layerGroupCode = layerGroupCodeProperty.GetValue(layer);
                    if (layerGroupCode?.ToString() == "sharedwaypoints") {
                        _sharedLayer = layer;
                        // Try to find Waypoints as property (public, then private)
                        _waypointsProperty = layer.GetType().GetProperty("Waypoints", BindingFlags.Public | BindingFlags.Instance)
                            ?? layer.GetType().GetProperty("Waypoints", BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        // If not a property, try as a field
                        FieldInfo? waypointsField = null;
                        if (_waypointsProperty == null) {
                            waypointsField = layer.GetType().GetField("Waypoints", BindingFlags.Public | BindingFlags.Instance)
                                ?? layer.GetType().GetField("Waypoints", BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (waypointsField != null) {
                                // Create a wrapper to access the field via reflection when needed
                                _waypointsProperty = null; // Clear property, we'll use field directly
                            }
                        }
                        
                        // Store field info if property not found
                        if (_waypointsProperty == null && waypointsField != null) {
                            _waypointsField = waypointsField;
                        }
                        
                        return _waypointsProperty != null || _waypointsField != null;
                    }
                }
            }

            return false;
        } catch (Exception e) {
            Logger.Warn($"Failed to detect VSCartographer: {e.Message}");
            return false;
        }
    }

    private Marker? ConvertWaypointToMarker(object waypoint) {
        try {
            // Access waypoint properties via reflection
            Type waypointType = waypoint.GetType();

            // Get Position - it's a public field, not a property
            FieldInfo? positionField = waypointType.GetField("Position", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? positionProperty = waypointType.GetProperty("Position", BindingFlags.Public | BindingFlags.Instance);
            if (positionField == null && positionProperty == null) {
                return null;
            }

            object? position = positionField != null
                ? positionField.GetValue(waypoint)
                : positionProperty?.GetValue(waypoint);
            if (position == null) {
                return null;
            }

            // Extract X and Z from Vec3d - check both fields and properties
            FieldInfo? xField = position.GetType().GetField("X", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? xProperty = position.GetType().GetProperty("X", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo? zField = position.GetType().GetField("Z", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? zProperty = position.GetType().GetProperty("Z", BindingFlags.Public | BindingFlags.Instance);
            if ((xField == null && xProperty == null) || (zField == null && zProperty == null)) {
                return null;
            }

            double x = Convert.ToDouble(
                xField != null 
                    ? xField.GetValue(position) ?? 0
                    : xProperty?.GetValue(position) ?? 0
            );
            double z = Convert.ToDouble(
                zField != null
                    ? zField.GetValue(position) ?? 0
                    : zProperty?.GetValue(position) ?? 0
            );
            Point point = new Point(x, z);

            // Get Title - check both field and property
            FieldInfo? titleField = waypointType.GetField("Title", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? titleProperty = waypointType.GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
            string title = titleField != null
                ? titleField.GetValue(waypoint)?.ToString() ?? "Unknown"
                : titleProperty?.GetValue(waypoint)?.ToString() ?? "Unknown";

            // Get Guid for unique ID - check both field and property
            FieldInfo? guidField = waypointType.GetField("Guid", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? guidProperty = waypointType.GetProperty("Guid", BindingFlags.Public | BindingFlags.Instance);
            string guid = guidField != null
                ? guidField.GetValue(waypoint)?.ToString() ?? Guid.NewGuid().ToString()
                : guidProperty?.GetValue(waypoint)?.ToString() ?? Guid.NewGuid().ToString();

            // Get OwningPlayerUid for tooltip - check both field and property
            FieldInfo? owningPlayerField = waypointType.GetField("OwningPlayerUid", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? owningPlayerProperty = waypointType.GetProperty("OwningPlayerUid", BindingFlags.Public | BindingFlags.Instance);
            string? owningPlayerUid = owningPlayerField != null
                ? owningPlayerField.GetValue(waypoint)?.ToString()
                : owningPlayerProperty?.GetValue(waypoint)?.ToString();
            
            // Convert player UID to player name
            string? owningPlayer = null;
            if (!string.IsNullOrEmpty(owningPlayerUid)) {
                try {
                    // First, try to find online player
                    var onlinePlayer = LiveMap.Api.Sapi.World.AllOnlinePlayers
                        .Cast<Vintagestory.API.Server.IServerPlayer>()
                        .FirstOrDefault(p => p.PlayerUID == owningPlayerUid);
                    
                    if (onlinePlayer != null) {
                        owningPlayer = onlinePlayer.PlayerName;
                    } else {
                        // Player not online, try to get from PlayerData
                        var playerData = LiveMap.Api.Sapi.PlayerData.GetPlayerDataByUid(owningPlayerUid);
                        if (playerData != null) {
                            // Try LastKnownPlayername as a field (not property)
                            FieldInfo? nameField = playerData.GetType().GetField("LastKnownPlayername", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            PropertyInfo? nameProperty = playerData.GetType().GetProperty("LastKnownPlayername", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            if (nameField != null) {
                                owningPlayer = nameField.GetValue(playerData)?.ToString();
                            } else if (nameProperty != null) {
                                owningPlayer = nameProperty.GetValue(playerData)?.ToString();
                            }
                        }
                    }
                } catch {
                    // Silently fail - just won't show player name
                }
            }

            // Create icon options
            IconOptions iconOptions = Config.IconOptions.DeepCopy();

            // Create tooltip
            TooltipOptions? tooltip = Config.Tooltip?.DeepCopy();
            if (tooltip?.Content != null) {
                string tooltipText;
                if (!string.IsNullOrEmpty(owningPlayer)) {
                    tooltipText = $"{title} (by {owningPlayer})";
                } else {
                    tooltipText = title;
                }
                tooltip.Content = string.Format(tooltip.Content, tooltipText);
            }

            // Create popup
            PopupOptions? popup = Config.Popup?.DeepCopy();
            if (popup?.Content != null) {
                if (!string.IsNullOrEmpty(owningPlayer)) {
                    popup.Content = $"{title}<br>Created by: {owningPlayer}";
                } else {
                    popup.Content = title;
                }
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
}
