namespace livemap.configuration;

public class Httpd {
    public bool Enabled { get; set; } = true;

    public int Port { get; set; } = 8080;

    /// <summary>
    /// The address to bind the web server to. If empty, the server will bind to all
    /// network interfaces (0.0.0.0 / *).
    /// Set this to a specific IP (e.g., "192.168.1.100") to bind directly
    /// to that address, which may avoid requiring admin privileges depending on the system configuration.
    /// </summary>
    public string BindAddress { get; set; } = "";
}
