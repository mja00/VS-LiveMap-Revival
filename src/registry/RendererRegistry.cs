using livemap.render;

namespace livemap.registry;

public class RendererRegistry() : Registry<Renderer>("renderers") {
    public static BasicRenderer? Basic { get; private set; }
    public static SepiaRenderer? Sepia { get; private set; }

    public void RegisterBuiltIns(LiveMap server) {
        Register(Basic = new BasicRenderer());
        Register(Sepia = new SepiaRenderer());

        // Initialize all renderers with server context
        foreach ((string _, Renderer renderer) in this) {
            renderer.Initialize(server);
        }
    }

    public override void Dispose() {
        foreach ((string _, Renderer renderer) in this) {
            renderer.Dispose();
        }

        Basic = null;
        Sepia = null;

        base.Dispose();
    }
}
