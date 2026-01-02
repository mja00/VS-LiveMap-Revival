namespace livemap.configuration;

public class Config {
    public bool DebugMode { get; set; } = false;

    public Httpd Httpd { get; set; } = new();

    public Web Web { get; set; } = new();

    public Zoom Zoom { get; set; } = new();

    public Ui Ui { get; set; } = new();

    public Layers Layers { get; set; } = new();

    public Render Render { get; set; } = new();
}
