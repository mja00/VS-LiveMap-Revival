using livemap.data;
using livemap.layer.marker.options.type;
using Newtonsoft.Json;

namespace livemap.layer.marker;

/// <summary>
///     The icon marker is used to display clickable/draggable icons on the map
/// </summary>
/// <remarks>
///     This is mostly used to mark specific points in the world such as players, translocators, traders, cities, etc.<br />
///     You can highlight entire areas instead of just a point with the <see cref="Polygon" />
/// </remarks>
public class Icon : Marker {
    /// <summary>
    ///     Create a new icon at 0,0 with a random id
    /// </summary>
    public Icon() : this(Guid.NewGuid().ToString(), new Point(0, 0)) {
    }

    /// <summary>
    ///     Create a new icon
    /// </summary>
    /// <param name="id">Unique identifying key</param>
    /// <param name="point">Absolute (not relative to spawn) world coordinates for the icon's anchor</param>
    /// <param name="options">Optional settings for the icon</param>
    public Icon(string id, Point point, IconOptions? options = null) : base("icon", id) {
        Point = point;
        Options = options;
    }

    /// <summary>
    ///     Absolute (not relative to spawn) world coordinates for the icon's anchor
    /// </summary>
    [JsonProperty(Order = -1)]
    public Point Point { get; set; }

    /// <summary>
    ///     Optional target point (relative to spawn) for translocators and similar markers
    /// </summary>
    [JsonProperty(Order = 0)]
    public Point? TargetPoint { get; set; }

    /// <inheritdoc cref="IconOptions" />
    [JsonProperty(Order = 10)]
    public new IconOptions? Options { get; set; }

    /// <inheritdoc cref="Marker.FromJson{T}" />
    public static Icon FromJson(string json) => FromJson<Icon>(json);
}
