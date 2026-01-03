using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace livemap.util;

public abstract class Files {
    public static readonly JsonSerializerSettings JsonSerializerMinifiedSettings = new() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };

    public static readonly JsonSerializerSettings JsonSerializerPrettySettings = new() { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Include, ContractResolver = new CamelCasePropertyNamesContractResolver() };
    public static string SavegameIdentifier { get; internal set; } = null!;
    public static string DataDir => Path.Combine(GamePaths.DataPath, "ModData", SavegameIdentifier, "LiveMap");
    public static string ColormapFile => Path.Combine(DataDir, "colormap.json");
    public static string WebDir => Path.Combine(DataDir, "web");
    public static string JsonDir => Path.Combine(WebDir, "data");
    public static string MarkerDir => Path.Combine(JsonDir, "markers");
    public static string TilesDir => Path.Combine(WebDir, "tiles");
    public static string GetColormapFile(int month) => Path.Combine(DataDir, $"colormap-{month}.json");

    internal static void ExtractWebFiles(LiveMap server) {
        GamePaths.EnsurePathExists(DataDir);
        // copy web assets from zip to disk
        // stored in "config" to allow the game to automatically load them for us
        foreach (IAsset asset in server.Sapi.Assets.GetMany("config", "livemap")) {
            // strip leading "config/" from the path
            string path = asset.Location.Path[7..];

            // ensure we actually have data
            if (asset.Data == null) {
                Logger.Error("error.files.loading-asset-from-zip".ToLang(path));
                continue;
            }

            // check if we've already saved this file to disk
            string destPath = Path.Combine(WebDir, path);
            if (File.Exists(destPath)) {
                if (server.Config.Web.ReadOnly) {
                    Logger.Debug("error.files.asset-already-exists".ToLang(path));
                    continue;
                }

                try {
                    byte[] existingData = File.ReadAllBytes(destPath);
                    if (existingData.SequenceEqual(asset.Data)) {
                        continue;
                    }
                } catch (Exception e) {
                    Logger.Debug($"Failed to read existing file '{destPath}', will overwrite: {e.Message}");
                }
            }

            try {
                Logger.Debug("success.files.saving-asset-to-disk".ToLang(path));
                GamePaths.EnsurePathExists(Path.GetDirectoryName(destPath));
                File.WriteAllBytes(destPath, asset.Data);
            } catch (Exception e) {
                Logger.Error("error.files.saving-asset-to-disk".ToLang(path));
                Logger.Error(e.ToString());
            }
        }
    }

    public static async Task WriteJsonAsync(string path, string json, CancellationToken cancellationToken) {
        FileInfo file = new(path);
        GamePaths.EnsurePathExists(file.Directory!.FullName);
        await File.WriteAllTextAsync(file.FullName, json, cancellationToken);
    }
}
