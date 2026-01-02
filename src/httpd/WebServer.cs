using System.Net;
using System.Text.RegularExpressions;
using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Engine;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.IO;
using livemap.util;

namespace livemap.httpd;

public partial class WebServer(LiveMap server)
{
    private IServerHost? _server;
    private volatile bool _running;
    private readonly LiveMap _serverContext = server;

    [GeneratedRegex(@"^(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?")]
    private static partial Regex FriendlyUrlRegex();

    public void Reload()
    {
        Dispose();
        // Allow time for the port to be released before binding again
        Thread.Sleep(100);
        Run();
    }

    public void Run()
    {
        if (!_serverContext.Config.Httpd.Enabled || _running)
        {
            return;
        }

        try
        {
            int port = _serverContext.Config.Httpd.Port;
            string bindAddress = _serverContext.Config.Httpd.BindAddress;

            // Validate port range
            if (port < 1 || port > 65535)
            {
                Logger.Error($"Invalid port {port}. Port must be between 1 and 65535.");
                _running = false;
                return;
            }

            // GenHTTP v10 API
            var host = Host.Create()
                .Handler(new FunctionalHandlerBuilder(HandleRequest));

            // Configure binding
            if (string.IsNullOrWhiteSpace(bindAddress))
            {
                host.Bind(IPAddress.Any, (ushort)port);
                Logger.Info($"Internal webserver starting on 0.0.0.0:{port}");
                LogAccessibleAddresses(port);
            }
            else
            {
                if (IPAddress.TryParse(bindAddress, out var ip))
                {
                    host.Bind(ip, (ushort)port);
                    Logger.Info($"Internal webserver starting on {ip}:{port}");
                }
                else
                {
                    Logger.Warn($"Invalid BindAddress '{bindAddress}', falling back to 0.0.0.0");
                    host.Bind(IPAddress.Any, (ushort)port);
                    Logger.Info($"Internal webserver starting on 0.0.0.0:{port}");
                    LogAccessibleAddresses(port);
                }
            }

            // Start the server - StartAsync() is called on the builder and returns IServerHost
            _server = host.StartAsync().AsTask().Result;
            _running = true;
            Logger.Info("Internal webserver successfully started");
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to start webserver: {e.Message}");
            _running = false;
            return;
        }
    }

    private ValueTask<IResponse?> HandleRequest(IRequest request)
    {
        try
        {
            string path = request.Target.Path.ToString();

            if (request.Method != RequestMethod.Get)
            {
                return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                              .Status(ResponseStatus.MethodNotAllowed)
                              .Content("Method Not Allowed")
                              .Type("text/plain")
                              .Build());
            }

            string urlLoc = path.Length > 1 ? path[1..] : "";

            try
            {
                MatchCollection matches = FriendlyUrlRegex().Matches(urlLoc);
                if (matches.Count > 0)
                {
                    string group6 = matches[0].Groups[6].Value;
                    if (group6.Length == 0 && !matches[0].Value.EndsWith('/'))
                    {
                        var original = request.Target.Path.ToString();
                        return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                                      .Header("Location", $"{original}/")
                                      .Status(ResponseStatus.MovedPermanently)
                                      .Build());
                    }
                    urlLoc = group6[1..];
                }
            }
            catch
            {
                // ignore
            }

            if (string.IsNullOrEmpty(urlLoc))
            {
                urlLoc = "index.html";
            }

            string filePath = Path.GetFullPath(Path.Combine(Files.WebDir, urlLoc));
            string webDirFull = Path.GetFullPath(Files.WebDir);

            // Reject path traversal attempts and direct directory access
            if (!filePath.StartsWith(webDirFull + Path.DirectorySeparatorChar))
            {
                return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                              .Status(ResponseStatus.Forbidden)
                              .Content("Forbidden")
                              .Type("text/plain")
                              .Build());
            }

            if (File.Exists(filePath))
            {
                string contentType = GetContentType(filePath);

                var resource = Resource.FromFile(filePath).Build();

                // Calculate ETag based on last modified time
                string? etag = null;
                try
                {
                    TimeSpan time = File.GetLastWriteTimeUtc(filePath) - DateTime.UnixEpoch;
                    etag = ((long)time.TotalMilliseconds).ToString();
                }
                catch
                {
                    // ignore ETag calculation errors
                }

                var response = AddCorsHeaders(request.Respond())
                              .Content(resource)
                              .Type(contentType)
                              .Status(ResponseStatus.Ok);

                if (etag != null)
                {
                    response.Header("ETag", etag);
                }

                return new ValueTask<IResponse?>(response.Build());
            }
            else
            {
                string notFoundPath = Path.Combine(Files.WebDir, "404.html");
                if (File.Exists(notFoundPath))
                {
                    var resource = Resource.FromFile(notFoundPath).Build();
                    return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                                  .Content(resource)
                                  .Status(ResponseStatus.NotFound)
                                  .Type("text/html")
                                  .Build());
                }

                return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                              .Status(ResponseStatus.NotFound)
                              .Content("404 Not Found")
                              .Type("text/plain")
                              .Build());
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Error handling request: {e.Message}");
            return new ValueTask<IResponse?>(AddCorsHeaders(request.Respond())
                          .Status(ResponseStatus.InternalServerError)
                          .Content("Internal Server Error")
                          .Build());
        }
    }

    private static IResponseBuilder AddCorsHeaders(IResponseBuilder response)
    {
        return response
            .Header("Access-Control-Allow-Origin", "*")
            .Header("Access-Control-Allow-Methods", "GET")
            .Header("Access-Control-Allow-Headers", "*");
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
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

    private class FunctionalHandlerBuilder(Func<IRequest, ValueTask<IResponse?>> handler) : IHandlerBuilder
    {
        private readonly Func<IRequest, ValueTask<IResponse?>> _handler = handler;

        public IHandler Build()
        {
            return new FunctionalHandler(_handler);
        }
    }

    private class FunctionalHandler(Func<IRequest, ValueTask<IResponse?>> handler) : IHandler
    {
        public ValueTask<IResponse?> HandleAsync(IRequest request)
        {
            return handler(request);
        }

        public ValueTask PrepareAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private static void LogAccessibleAddresses(int port)
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            Logger.Info("You should be able to access the map at:");
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    Logger.Info($"\thttp://{ip}:{port}/");
                }
            }
        }
        catch
        {
            // ignore DNS errors
        }
    }

    public void Dispose()
    {
        try
        {
            if (_server != null)
            {
                _server.StopAsync().AsTask().Wait();
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"Exception while disposing web server: {ex}");
        }

        _server = null;
        _running = false;
    }
}
