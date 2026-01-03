using livemap.data;
using Newtonsoft.Json;

namespace livemap.layer.marker.options.type;

/// <summary>
///     Optional settings for the <see cref="Icon" /> marker
/// </summary>
public class IconOptions : InteractiveLayerOptions {
    /// <summary>
    ///     Text for the browser tooltip that appear on marker hover (no tooltip by default)
    /// </summary>
    /// <remarks>
    ///     Useful for accessibility
    /// </remarks>
    /// <seealso href="https://leafletjs.com/examples/accessibility/#markers-must-be-labelled">Leaflet Documentation</seealso>
    [JsonProperty(Order = 0)]
    public string? Title { get; set; }

    /// <summary>
    ///     Text for the alt attribute of the icon image
    /// </summary>
    /// <remarks>
    ///     Useful for accessibility
    /// </remarks>
    /// <seealso href="https://leafletjs.com/examples/accessibility/#markers-must-be-labelled">Leaflet Documentation</seealso>
    [JsonProperty(Order = 1)]
    public string? Alt { get; set; }

    /// <summary>
    ///     The URL to the icon image (absolute or relative to your map url)
    /// </summary>
    [JsonProperty(Order = 2)]
    public string? IconUrl { get; set; }

    /// <summary>
    ///     The URL to a retina sized version of the icon image (absolute or relative to your map url)
    /// </summary>
    /// <remarks>
    ///     Used for Retina screen devices
    /// </remarks>
    [JsonProperty(Order = 3)]
    public string? IconRetinaUrl { get; set; }

    /// <summary>
    ///     Size of the icon image in pixels
    /// </summary>
    [JsonProperty(Order = 4)]
    public Point? IconSize { get; set; }

    /// <summary>
    ///     The coordinates of the "tip" of the icon (relative to its top left corner)
    /// </summary>
    /// <remarks>
    ///     The icon will be aligned so that this point is at the icon's geographical location. Centered by default if size is specified
    /// </remarks>
    [JsonProperty(Order = 5)]
    public Point? IconAnchor { get; set; }

    /// <summary>
    ///     Rotation angle, in degrees, clockwise
    /// </summary>
    [JsonProperty(Order = 6)]
    public double? RotationAngle { get; set; }

    /// <summary>
    ///     The rotation center, as a transform-origin CSS rule
    /// </summary>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/CSS/transform-origin">MDN Web Docs</seealso>
    [JsonProperty(Order = 7)]
    public string? RotationOrigin { get; set; }

    /// <summary>
    ///     The coordinates of the point from which popups will "open", relative to the icon anchor
    /// </summary>
    [JsonProperty(Order = 8)]
    public Point? PopupAnchor { get; set; }

    /// <summary>
    ///     The coordinates of the point from which tooltips will "open", relative to the icon anchor
    /// </summary>
    [JsonProperty(Order = 9)]
    public Point? TooltipAnchor { get; set; }

    /// <summary>
    ///     The URL to the icon shadow image
    /// </summary>
    /// <remarks>
    ///     If not specified, no shadow image will be created
    /// </remarks>
    [JsonProperty(Order = 10)]
    public string? ShadowUrl { get; set; }

    /// <summary>
    ///     The URL to a retina sized version of the icon shadow image
    /// </summary>
    /// <remarks>
    ///     Used for Retina screen devices
    /// </remarks>
    [JsonProperty(Order = 11)]
    public string? ShadowRetinaUrl { get; set; }

    /// <summary>
    ///     Size of the shadow image in pixels
    /// </summary>
    [JsonProperty(Order = 12)]
    public Point? ShadowSize { get; set; }

    /// <summary>
    ///     The coordinates of the "tip" of the shadow (relative to its top left corner)
    /// </summary>
    /// <remarks>
    ///     Defaults to the same as <see cref="IconAnchor" /> if not specified
    /// </remarks>
    [JsonProperty(Order = 13)]
    public Point? ShadowAnchor { get; set; }

    /// <summary>
    ///     Map pane where the icon's shadow will be added
    /// </summary>
    [JsonProperty(Order = 14)]
    public string? ShadowPane { get; set; }

    /// <summary>
    ///     Whether the crossOrigin attribute will be added to the tiles
    /// </summary>
    /// <remarks>
    ///     If a String is provided, all tiles will have their crossOrigin attribute set to the String provided.<br />
    ///     This is needed if you want to access tile pixel data.<br />
    ///     Refer to CORS Settings for valid String values
    /// </remarks>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/crossorigin">MDN Web Docs</seealso>
    [JsonProperty(Order = 15)]
    public string? CrossOrigin { get; set; }

    /// <summary>
    ///     The icon's z-index offset
    /// </summary>
    /// <remarks>
    ///     By default, icons z-index is set automatically based on its latitude.<br />
    ///     Use this option if you want to put the marker on top of all others (or below), specifying a high value like 1000 (or high negative value, respectively)
    /// </remarks>
    [JsonProperty(Order = 16)]
    public double? ZIndexOffset { get; set; }

    /// <summary>
    ///     The opacity of the icon
    /// </summary>
    [JsonProperty(Order = 17)]
    public double? Opacity { get; set; }

    /// <summary>
    ///     If <see langword="true" />, the icon will get on top of others when you hover the mouse over it
    /// </summary>
    [JsonProperty(Order = 18)]
    public bool? RiseOnHover { get; set; }

    /// <summary>
    ///     The z-index offset used for the <see cref="RiseOnHover" /> feature
    /// </summary>
    [JsonProperty(Order = 19)]
    public double? RiseOffset { get; set; }

    /// <summary>
    ///     Whether the marker can be tabbed to with a keyboard and clicked by pressing enter
    /// </summary>
    [JsonProperty(Order = 20)]
    public bool? Keyboard { get; set; }

    /// <summary>
    ///     Whether the icon is draggable with mouse/touch or not
    /// </summary>
    [JsonProperty(Order = 21)]
    public bool? Draggable { get; set; }

    /// <summary>
    ///     Whether to pan the map when dragging this icon near it's edge or not
    /// </summary>
    [JsonProperty(Order = 22)]
    public bool? AutoPan { get; set; }

    /// <summary>
    ///     Distance (in pixels to the left/right and to the top/bottom) of the map edge to start panning the map
    /// </summary>
    [JsonProperty(Order = 23)]
    public Point? AutoPanPadding { get; set; }

    /// <summary>
    ///     Number of pixels the map should pan by
    /// </summary>
    [JsonProperty(Order = 24)]
    public double? AutoPanSpeed { get; set; }

    /// <summary>
    ///     When <see langword="true" />, the map will pan whenever the icon is focused (via e.g. pressing tab on the keyboard) to ensure the icon is visible within the map's bounds
    /// </summary>
    [JsonProperty(Order = 25)]
    public bool? AutoPanOnFocus { get; set; }

    /// <summary>
    ///     Color for SVG icons (hex string, e.g. "#FF0000")
    /// </summary>
    /// <remarks>
    ///     Applied as inline style to SVG elements
    /// </remarks>
    [JsonProperty(Order = 26)]
    public string? Color { get; set; }
}
