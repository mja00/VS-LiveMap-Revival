using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using GenHTTP.Api.Content;
using GenHTTP.Api.Content.IO;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.IO;
using livemap.util;

namespace livemap.httpd;

public partial class WebServer(LiveMap server) {
    // Cache configuration
    private const long _maxCacheSizeBytes = 100 * 1024 * 1024; // 100MB
    private const int _maxCacheFiles = 500;
    private static readonly ConcurrentDictionary<string, CachedFile> _fileCache = new();
    private static long _totalCacheSizeBytes;
    private static readonly object _cacheLock = new();
    private volatile bool _running;
    private IServerHost? _server;

    [GeneratedRegex(@"^(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?")]
    private static partial Regex FriendlyUrlRegex();

    public void Reload() {
        Dispose();
        // Allow time for the port to be released before binding again
        Thread.Sleep(100);
        Run();
    }

    public void Run() {
        if (!server.Config.Httpd.Enabled || _running) {
            return;
        }

        try {
            int port = server.Config.Httpd.Port;
            string bindAddress = server.Config.Httpd.BindAddress;

            // Validate port range
            if (port is < 1 or > 65535) {
                Logger.Error("webserver.invalid-port".ToLang(port));
                _running = false;
                return;
            }

            // GenHTTP v10 API
            IServerHost host = Host.Create()
                .Handler(new FunctionalHandlerBuilder(HandleRequest));

            // Configure binding
            if (string.IsNullOrWhiteSpace(bindAddress)) {
                host.Bind(IPAddress.Any, (ushort)port);
                Logger.Info("webserver.starting".ToLang("0.0.0.0", port));
                LogAccessibleAddresses(port);
            } else {
                if (IPAddress.TryParse(bindAddress, out IPAddress? ip)) {
                    host.Bind(ip, (ushort)port);
                    Logger.Info("webserver.starting".ToLang(ip, port));
                } else {
                    Logger.Warn("webserver.invalid-bind".ToLang(bindAddress));
                    host.Bind(IPAddress.Any, (ushort)port);
                    Logger.Info("webserver.starting".ToLang("0.0.0.0", port));
                    LogAccessibleAddresses(port);
                }
            }

            // Start the server - StartAsync() is called on the builder and returns IServerHost
            _server = host.StartAsync().AsTask().Result;
            _running = true;
            Logger.Info("webserver.started".ToLang());
        } catch (Exception e) {
            Logger.Error("webserver.failed".ToLang(e.Message));
            _running = false;
        }
    }

    private ValueTask<IResponse?> HandleRequest(IRequest request) {
        try {
            string path = request.Target.Path.ToString();

            if (request.Method != RequestMethod.Get) {
                return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                    .Status(ResponseStatus.MethodNotAllowed)
                    .Content("Method Not Allowed")
                    .Type("text/plain")
                    .Build());
            }

            string urlLoc = path.Length > 1 ? path[1..] : "";

            try {
                MatchCollection matches = FriendlyUrlRegex().Matches(urlLoc);
                if (matches.Count > 0) {
                    string group6 = matches[0].Groups[6].Value;
                    if (group6.Length == 0 && !matches[0].Value.EndsWith('/')) {
                        string original = request.Target.Path.ToString();
                        return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                            .Header("Location", $"{original}/")
                            .Status(ResponseStatus.MovedPermanently)
                            .Build());
                    }

                    urlLoc = group6[1..];
                }
            } catch (Exception e) {
                Logger.Warn($"Failed to parse friendly URL '{urlLoc}': {e.Message}");
            }

            if (string.IsNullOrEmpty(urlLoc)) {
                urlLoc = "index.html";
            }

            string filePath = Path.GetFullPath(Path.Combine(Files.WebDir, urlLoc));
            string webDirFull = Path.GetFullPath(Files.WebDir);

            // Reject path traversal attempts and direct directory access
            if (!filePath.StartsWith(webDirFull + Path.DirectorySeparatorChar)) {
                return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                    .Status(ResponseStatus.Forbidden)
                    .Content("Forbidden")
                    .Type("text/plain")
                    .Build());
            }

            if (File.Exists(filePath)) {
                bool isTile = IsTileFile(filePath);
                CachedFile? cachedFile;

                if (isTile) {
                    // Try to get from cache first (only for tiles)
                    cachedFile = GetCachedFile(filePath);

                    // If not in cache or invalid, load from disk and cache
                    if (cachedFile == null) {
                        try {
                            Logger.Debug($"Cache miss: Loading tile '{urlLoc}' from disk");
                        } catch {
                            // Ignore logger errors in unit tests or when Logger isn't initialized
                        }

                        cachedFile = LoadAndCacheFile(filePath);
                    } else {
                        try {
                            Logger.Debug($"Cache hit: Serving tile '{urlLoc}' from cache");
                        } catch {
                            // Ignore logger errors in unit tests or when Logger isn't initialized
                        }
                    }
                } else {
                    // For non-tile files (JSON, HTML, etc.), serve directly from disk without caching
                    try {
                        Logger.Debug($"Serving non-tile file '{urlLoc}' directly from disk (not cached)");
                    } catch {
                        // Ignore logger errors in unit tests or when Logger isn't initialized
                    }

                    cachedFile = LoadFileFromDisk(filePath);
                }

                // Create resource from data
                IResource resource = new CachedResource(cachedFile.Data, Path.GetFileName(filePath));

                IResponseBuilder response = AddCorsHeaders(request.Respond())
                    .Content(resource)
                    .Type(cachedFile.ContentType)
                    .Status(ResponseStatus.Ok);

                if (cachedFile.ETag != null) {
                    response.Header("ETag", cachedFile.ETag);
                }

                return new ValueTask<IResponse?>(response.Build());
            }

            string notFoundPath = Path.Combine(Files.WebDir, "404.html");
            if (File.Exists(notFoundPath)) {
                // 404.html is not a tile, serve directly from disk without caching
                try {
                    Logger.Debug("Serving '404.html' directly from disk (not cached)");
                } catch {
                    // Ignore logger errors in unit tests or when Logger isn't initialized
                }

                CachedFile cachedFile = LoadFileFromDisk(notFoundPath);
                IResource resource = new CachedResource(cachedFile.Data, "404.html");

                return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                    .Content(resource)
                    .Status(ResponseStatus.NotFound)
                    .Type(cachedFile.ContentType)
                    .Build());
            }

            return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                .Status(ResponseStatus.NotFound)
                .Content("404 Not Found")
                .Type("text/plain")
                .Build());
        } catch (Exception e) {
            Logger.Error($"Error handling request: {e.Message}");
            return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                .Status(ResponseStatus.InternalServerError)
                .Content("Internal Server Error")
                .Build());
        }
    }

    private static IResponseBuilder AddCorsHeaders(IResponseBuilder response) {
        return response
            .Header("Access-Control-Allow-Origin", "*")
            .Header("Access-Control-Allow-Methods", "GET")
            .Header("Access-Control-Allow-Headers", "*");
    }

    internal static bool IsTileFile(string filePath) {
        // Check if the file is in the tiles directory
        try {
            string tilesDirFull = Path.GetFullPath(Files.TilesDir);
            string filePathFull = Path.GetFullPath(filePath);
            return filePathFull.StartsWith(tilesDirFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        } catch {
            // If we can't access Files.TilesDir (e.g., in unit tests), check if path contains "tiles"
            // This is a fallback for testing scenarios
            string filePathLower = filePath.ToLowerInvariant();
            return filePathLower.Contains(Path.DirectorySeparatorChar + "tiles" + Path.DirectorySeparatorChar) ||
                   filePathLower.Contains(Path.AltDirectorySeparatorChar + "tiles" + Path.AltDirectorySeparatorChar);
        }
    }

    private static string GetContentType(string path) {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch {
            ".html" or ".htm" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".json" => "application/json",
            ".ico" => "image/x-icon",
            ".svg" => "image/svg+xml",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".txt" => "text/plain",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }

    internal static CachedFile? GetCachedFile(string filePath) {
        if (!_fileCache.TryGetValue(filePath, out CachedFile? cachedFile)) {
            return null;
        }

        // Check if file on disk is newer than cached version
        try {
            DateTime currentWriteTime = File.GetLastWriteTimeUtc(filePath);
            if (currentWriteTime > cachedFile.LastWriteTime) {
                // File has been modified, invalidate cache
                try {
                    Logger.Debug($"Cache invalidated: File '{Path.GetFileName(filePath)}' was modified on disk");
                } catch {
                    // Ignore logger errors in unit tests or when Logger isn't initialized
                }

                lock (_cacheLock) {
                    if (_fileCache.TryRemove(filePath, out _)) {
                        _totalCacheSizeBytes -= cachedFile.Data.Length;
                    }
                }

                return null;
            }
        } catch {
            // If we can't check the file, invalidate the cache entry
            try {
                Logger.Debug($"Cache invalidated: Unable to check file '{Path.GetFileName(filePath)}' modification time");
            } catch {
                // Ignore logger errors in unit tests or when Logger isn't initialized
            }

            lock (_cacheLock) {
                if (_fileCache.TryRemove(filePath, out _)) {
                    _totalCacheSizeBytes -= cachedFile.Data.Length;
                }
            }

            return null;
        }

        // Update last access time (for LRU eviction)
        // Use a retry loop to handle concurrent updates from other threads
        // This ensures the LastAccessTime is properly updated for LRU eviction accuracy
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++) {
            CachedFile updatedFile = cachedFile with { LastAccessTime = DateTime.UtcNow };
            if (_fileCache.TryUpdate(filePath, updatedFile, cachedFile)) {
                return updatedFile;
            }

            // TryUpdate failed - another thread modified the entry
            // Re-read the current value and retry
            if (!_fileCache.TryGetValue(filePath, out cachedFile)) {
                // Entry was removed, return null
                return null;
            }
        }

        // If all retries failed, return the current cached value
        // This is a best-effort approach - the LastAccessTime may be slightly stale
        // but the data, content type, and ETag are still correct
        return cachedFile;
    }

    /// <summary>
    ///     Loads a file from disk and creates a CachedFile with ETag calculation.
    ///     This is a shared helper to avoid code duplication for file loading and ETag calculation.
    /// </summary>
    private static CachedFile LoadFileFromDisk(string filePath) {
        byte[] data = File.ReadAllBytes(filePath);
        string contentType = GetContentType(filePath);
        DateTime lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        // Calculate ETag based on last modified time
        string? etag = null;
        try {
            TimeSpan time = lastWriteTime - DateTime.UnixEpoch;
            etag = ((long)time.TotalMilliseconds).ToString();
        } catch (Exception e) {
            Logger.Warn($"Failed to calculate ETag for '{filePath}': {e.Message}");
        }

        return new CachedFile(data, contentType, etag, lastWriteTime, DateTime.UtcNow);
    }

    internal static CachedFile LoadAndCacheFile(string filePath) {
        CachedFile cachedFile = LoadFileFromDisk(filePath);

        // Evict if necessary before adding
        EvictIfNeeded(cachedFile.Data.Length);

        // Add to cache atomically with size tracking
        // Both TryAdd and size increment must be done within the lock to prevent race conditions
        // where another thread could remove the entry between TryAdd and size increment
        lock (_cacheLock) {
            if (!_fileCache.TryAdd(filePath, cachedFile)) {
                return cachedFile;
            }

            _totalCacheSizeBytes += cachedFile.Data.Length;

            try {
                Logger.Debug($"Cached file '{Path.GetFileName(filePath)}' ({cachedFile.Data.Length} bytes, {_fileCache.Count} files, {_totalCacheSizeBytes / 1024}KB total)");
            } catch {
                // Ignore logger errors in unit tests or when Logger isn't initialized
            }
        }

        return cachedFile;
    }

    internal static void EvictIfNeeded(long newFileSize) {
        lock (_cacheLock) {
            // Keep evicting until we have enough space or hit file count limit
            while ((_totalCacheSizeBytes + newFileSize > _maxCacheSizeBytes || _fileCache.Count >= _maxCacheFiles) && _fileCache.Count > 0) {
                // Create a snapshot of cache entries with their access times while holding the lock
                // This ensures we have a consistent view and prevents race conditions with TryUpdate
                // in GetCachedFile, which can modify LastAccessTime concurrently
                List<(string Key, DateTime LastAccessTime, int DataLength)> snapshot = new(_fileCache.Count);
                snapshot.AddRange(_fileCache.Select(entry => (entry.Key, entry.Value.LastAccessTime, entry.Value.Data.Length)));

                if (snapshot.Count == 0) {
                    break;
                }

                // Find the least recently used file from the snapshot
                string? lruKey = null;
                DateTime lruTime = DateTime.MaxValue;

                foreach ((string key, DateTime lastAccessTime, _) in snapshot) {
                    if (lastAccessTime >= lruTime) {
                        continue;
                    }

                    lruTime = lastAccessTime;
                    lruKey = key;
                }

                // Remove the LRU entry
                // Note: TryRemove may fail if the entry was removed by another thread (e.g., file invalidation).
                // In that case, the size was already adjusted when the other thread removed it, so we just break.
                if (lruKey != null && _fileCache.TryRemove(lruKey, out CachedFile? removed)) {
                    try {
                        Logger.Debug($"Cache eviction: Removed '{Path.GetFileName(lruKey)}' (LRU, {removed.Data.Length} bytes)");
                    } catch {
                        // Ignore logger errors in unit tests or when Logger isn't initialized
                    }

                    _totalCacheSizeBytes -= removed.Data.Length;
                } else {
                    // Entry was removed by another thread (e.g., file invalidation in GetCachedFile)
                    // The size was already adjusted when that thread removed it, so we just break
                    // If we can't remove anything, break to avoid infinite loop
                    break;
                }
            }
        }
    }

    private static void LogAccessibleAddresses(int port) {
        try {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            Logger.Info("webserver.ips".ToLang());
            foreach (IPAddress ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)) {
                Logger.Info($"\thttp://{ip}:{port}/");
            }
        } catch (Exception e) {
            Logger.Warn($"Failed to resolve host addresses: {e.Message}");
        }
    }

    public void Dispose() {
        try {
            if (_server != null) {
                _server.StopAsync().AsTask().Wait();
            }
        } catch (Exception ex) {
            Logger.Info($"Exception while disposing web server: {ex}");
        }

        // Clear cache on dispose
        lock (_cacheLock) {
            _fileCache.Clear();
            _totalCacheSizeBytes = 0;
        }

        _server = null;
        _running = false;
    }

    internal record CachedFile(byte[] Data, string ContentType, string? ETag, DateTime LastWriteTime, DateTime LastAccessTime);

    private class FunctionalHandlerBuilder(Func<IRequest, ValueTask<IResponse?>> handler) : IHandlerBuilder {
        public IHandler Build() => new FunctionalHandler(handler);
    }

    private class FunctionalHandler(Func<IRequest, ValueTask<IResponse?>> handler) : IHandler {
        public ValueTask<IResponse?> HandleAsync(IRequest request) => handler(request);

        public ValueTask PrepareAsync() => ValueTask.CompletedTask;
    }

    internal class CachedResource(byte[] data, string name) : IResource {
        public string Name => name;

        public DateTime? Modified => null;

        public ulong? Length => (ulong)data.Length;

        public FlexibleContentType? ContentType => null;

        /// <summary>
        ///     Returns a read-only MemoryStream wrapping the cached byte array.
        ///     Each call creates a new MemoryStream instance, which is safe for concurrent reads
        ///     since the underlying byte array is immutable from this resource's perspective.
        ///     The writable parameter is set to false to prevent accidental modification.
        ///     Note: Each request creates a new CachedResource instance, so concurrent requests
        ///     operate on separate instances (though they may reference the same cached byte array).
        /// </summary>
        public ValueTask<Stream> GetContentAsync() => new(new MemoryStream(data, false));

        public ValueTask<ulong> CalculateChecksumAsync() => new(0);

        /// <summary>
        ///     Writes the cached data to the target stream using the specified buffer size.
        ///     The data is written in chunks to respect the buffer size parameter and avoid
        ///     large single writes that could block the thread pool.
        /// </summary>
        public async ValueTask WriteAsync(Stream target, uint bufferSize) {
            if (data.Length == 0) {
                return;
            }

            // Use a reasonable default if bufferSize is 0 or too small
            int effectiveBufferSize = bufferSize > 0 ? (int)bufferSize : 8192;
            int offset = 0;
            int remaining = data.Length;

            while (remaining > 0) {
                int chunkSize = Math.Min(effectiveBufferSize, remaining);
                await target.WriteAsync(data, offset, chunkSize);
                offset += chunkSize;
                remaining -= chunkSize;
            }
        }
    }
}
