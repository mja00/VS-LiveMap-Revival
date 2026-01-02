using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace livemap.command.subcommand;

public class QueueCmd(LiveMap server) : AbstractCommand(server, ["queue"]) {
    public override TextCommandResult Execute(TextCommandCallingArgs args) {
        if (_server.RenderTaskManager == null) {
            return "queue.not-active".CommandSuccess();
        }

        (int buffer, int process) = _server.RenderTaskManager.GetCounts();
        string status = _server.RenderTaskManager.IsRunning ? "queue.status-running".ToLang() : "queue.status-stopped".ToLang();

        return "queue.response".CommandSuccess(status, buffer, process, _server.Colormap.Count);
    }
}
