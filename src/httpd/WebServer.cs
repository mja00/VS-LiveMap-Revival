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

public partial class WebServer
{
    private IServer? _server;
    private volatile bool _running;
    private readonly LiveMap _serverContext;

    [GeneratedRegex(@"^(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?")]
    private static partial Regex FriendlyUrlRegex();

    public WebServer(LiveMap server)
    {
        _serverContext = server;
    }

    public void Reload()
    {
        Dispose();
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

            _server = host.Build();

            // Start the server if it implements IServerHost
            if (_server is IServerHost hostServer)
            {
                hostServer.StartAsync().AsTask().Wait();
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to start webserver: {e.Message}");
            _running = false;
            return;
        }

        if (_server != null)
        {

            try
            {
                // Using reflection to find Start method to be safe if I don't know the exact interface
                var startMethod = _server.GetType().GetMethod("StartAsync");
                if (startMethod != null)
                {
                    var task = (ValueTask)startMethod.Invoke(_server, null)!;
                    task.AsTask().Wait();
                }
                else
                {
                    // Try Start
                    _server.GetType().GetMethod("Start")?.Invoke(_server, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error starting server: {ex.Message}");
            }
        }

        _running = true;
        Logger.Info("Internal webserver successfully started");
    }

    private ValueTask<IResponse?> HandleRequest(IRequest request)
    {
        try
        {
            string path = request.Target.Path.ToString();

            if (request.Method != RequestMethod.Get)
            {
                return new ValueTask<IResponse?>(request.Respond()
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
                        return new ValueTask<IResponse?>(request.Respond()
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

            if (!filePath.StartsWith(webDirFull + Path.DirectorySeparatorChar) && filePath != webDirFull)
            {
                return new ValueTask<IResponse?>(request.Respond()
                              .Status(ResponseStatus.Forbidden)
                              .Content("Forbidden")
                              .Type("text/plain")
                              .Build());
            }

            if (File.Exists(filePath))
            {
                string contentType = GetContentType(filePath);

                var resource = Resource.FromFile(filePath).Build();

                return new ValueTask<IResponse?>(request.Respond()
                              .Content(resource)
                              .Type(contentType)
                              .Status(ResponseStatus.Ok)
                              .Build());
            }
            else
            {
                string notFoundPath = Path.Combine(Files.WebDir, "404.html");
                if (File.Exists(notFoundPath))
                {
                    var resource = Resource.FromFile(notFoundPath).Build();
                    return new ValueTask<IResponse?>(request.Respond()
                                  .Content(resource)
                                  .Status(ResponseStatus.NotFound)
                                  .Type("text/html")
                                  .Build());
                }

                return new ValueTask<IResponse?>(request.Respond()
                              .Status(ResponseStatus.NotFound)
                              .Content("404 Not Found")
                              .Type("text/plain")
                              .Build());
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Error handling request: {e.Message}");
            return new ValueTask<IResponse?>(request.Respond()
                          .Status(ResponseStatus.InternalServerError)
                          .Content("Internal Server Error")
                          .Build());
        }
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

        public IHandler Build() // Confirmed via Probe
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
            if (_server is IDisposable d) d.Dispose();
        }
        catch { }

        _server = null;
        _running = false;
        GC.SuppressFinalize(this);
    }
}
