using livemap.util;
using Vintagestory.API.Common;

namespace livemap.command.subcommand;

public class QueueCmd(LiveMap server) : AbstractCommand(server, ["queue"]) {
    public override TextCommandResult Execute(TextCommandCallingArgs args) {
        if (_server.RenderTaskManager == null) {
            return TextCommandResult.Success("Render task manager is not active.");
        }

        (int buffer, int process) = _server.RenderTaskManager.GetCounts();
        string status = _server.RenderTaskManager.IsRunning ? "Running" : "Stopped";
        int colormapCount = _server.Colormap.Count;

        return TextCommandResult.Success($"Render Queue Status: {status}\nBuffer Queue: {buffer}\nProcess Queue: {process}\nColormap Entries: {colormapCount}");
    }
}
