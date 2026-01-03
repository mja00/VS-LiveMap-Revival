using livemap.layer.marker.options;
using livemap.layer.marker.options.type;
using Point = livemap.data.Point;

namespace livemap.configuration;

public class VSCartographer {
    public bool Enabled { get; set; } = true;

    public int UpdateInterval { get; set; } = 30;

    public bool DefaultShowLayer { get; set; } = true;

    public IconOptions IconOptions { get; set; } = new() {
        Title = "",
        Alt = "",
        IconUrl = "#svg-marker",
        IconSize = new Point(16, 16),
        Pane = "vscartographer"
    };

    public TooltipOptions? Tooltip { get; set; } = new() { Direction = "top", Content = "{0}" };

    public PopupOptions? Popup { get; set; }

    public string? Css { get; set; } = ".leaflet-vscartographer-pane .leaflet-marker-icon{filter:drop-shadow(1px 0 0 rgba(0,0,0,0.8)) drop-shadow(-1px 0 0 rgba(0,0,0,0.8)) drop-shadow(0 1px 0 rgba(0,0,0,0.8)) drop-shadow(0 -1px 0 rgba(0,0,0,0.8)) drop-shadow(0 0 2px rgba(0,0,0,0.5))}";
}
