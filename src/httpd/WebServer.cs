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
    private readonly LiveMap _serverContext = server;
    private volatile bool _running;
    private IServerHost? _server;

    // Cache configuration
    private const long MaxCacheSizeBytes = 100 * 1024 * 1024; // 100MB
    private const int MaxCacheFiles = 500;
    private static readonly ConcurrentDictionary<string, CachedFile> _fileCache = new();
    private static long _totalCacheSizeBytes = 0;
    private static readonly object _cacheLock = new();

    private record CachedFile(byte[] Data, string ContentType, string? ETag, DateTime LastWriteTime, DateTime LastAccessTime);

    [GeneratedRegex(@"^(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?")]
    private static partial Regex FriendlyUrlRegex();

    public void Reload() {
        Dispose();
        // Allow time for the port to be released before binding again
        Thread.Sleep(100);
        Run();
    }

    public void Run() {
        if (!_serverContext.Config.Httpd.Enabled || _running) {
            return;
        }

        try {
            int port = _serverContext.Config.Httpd.Port;
            string bindAddress = _serverContext.Config.Httpd.BindAddress;

            // Validate port range
            if (port < 1 || port > 65535) {
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
                // Try to get from cache first
                CachedFile? cachedFile = GetCachedFile(filePath);

                // If not in cache or invalid, load from disk and cache
                if (cachedFile == null) {
                    cachedFile = LoadAndCacheFile(filePath);
                }

                // Create resource from cached data
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
                // Try to get from cache first
                CachedFile? cachedFile = GetCachedFile(notFoundPath);

                // If not in cache or invalid, load from disk and cache
                if (cachedFile == null) {
                    cachedFile = LoadAndCacheFile(notFoundPath);
                }

                // Create resource from cached data
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

    private static CachedFile? GetCachedFile(string filePath) {
        if (!_fileCache.TryGetValue(filePath, out CachedFile? cachedFile)) {
            return null;
        }

        // Check if file on disk is newer than cached version
        try {
            DateTime currentWriteTime = File.GetLastWriteTimeUtc(filePath);
            if (currentWriteTime > cachedFile.LastWriteTime) {
                // File has been modified, invalidate cache
                _fileCache.TryRemove(filePath, out _);
                lock (_cacheLock) {
                    _totalCacheSizeBytes -= cachedFile.Data.Length;
                }
                return null;
            }
        } catch {
            // If we can't check the file, invalidate the cache entry
            _fileCache.TryRemove(filePath, out _);
            lock (_cacheLock) {
                _totalCacheSizeBytes -= cachedFile.Data.Length;
            }
            return null;
        }

        // Update last access time (for LRU eviction)
        CachedFile updatedFile = cachedFile with { LastAccessTime = DateTime.UtcNow };
        _fileCache.TryUpdate(filePath, updatedFile, cachedFile);

        return updatedFile;
    }

    private static CachedFile LoadAndCacheFile(string filePath) {
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

        CachedFile cachedFile = new(data, contentType, etag, lastWriteTime, DateTime.UtcNow);

        // Evict if necessary before adding
        EvictIfNeeded(data.Length);

        // Add to cache
        if (_fileCache.TryAdd(filePath, cachedFile)) {
            lock (_cacheLock) {
                _totalCacheSizeBytes += data.Length;
            }
        }

        return cachedFile;
    }

    private static void EvictIfNeeded(long newFileSize) {
        lock (_cacheLock) {
            // Keep evicting until we have enough space or hit file count limit
            while ((_totalCacheSizeBytes + newFileSize > MaxCacheSizeBytes || _fileCache.Count >= MaxCacheFiles) && _fileCache.Count > 0) {
                // Find the least recently used file
                string? lruKey = null;
                DateTime lruTime = DateTime.MaxValue;

                foreach (KeyValuePair<string, CachedFile> entry in _fileCache) {
                    if (entry.Value.LastAccessTime < lruTime) {
                        lruTime = entry.Value.LastAccessTime;
                        lruKey = entry.Key;
                    }
                }

                // Remove the LRU entry
                if (lruKey != null && _fileCache.TryRemove(lruKey, out CachedFile? removed)) {
                    _totalCacheSizeBytes -= removed.Data.Length;
                } else {
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

    private class FunctionalHandlerBuilder(Func<IRequest, ValueTask<IResponse?>> handler) : IHandlerBuilder {
        private readonly Func<IRequest, ValueTask<IResponse?>> _handler = handler;

        public IHandler Build() => new FunctionalHandler(_handler);
    }

    private class FunctionalHandler(Func<IRequest, ValueTask<IResponse?>> handler) : IHandler {
        public ValueTask<IResponse?> HandleAsync(IRequest request) => handler(request);

        public ValueTask PrepareAsync() => ValueTask.CompletedTask;
    }

    internal class CachedResource(byte[] data, string name) : IResource {
        public string? Name => name;

        public DateTime? Modified => null;

        public ulong? Length => (ulong)data.Length;

        public FlexibleContentType? ContentType => null;

        public ValueTask<Stream> GetContentAsync() => new(new MemoryStream(data, false));

        public ValueTask<ulong> CalculateChecksumAsync() => new(0);

        public ValueTask WriteAsync(Stream target, uint bufferSize) {
            target.Write(data, 0, data.Length);
            return ValueTask.CompletedTask;
        }
    }
}
