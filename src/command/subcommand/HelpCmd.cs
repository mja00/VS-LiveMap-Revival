using System.Text;
using livemap.util;
using Vintagestory.API.Common;

namespace livemap.command.subcommand;

public class HelpCmd(LiveMap server) : AbstractCommand(server, ["help"]) {
    public override TextCommandResult Execute(TextCommandCallingArgs args) {
        StringBuilder sb = new();
        sb.AppendLine("Available Commands:");

        foreach (AbstractCommand cmd in _server.CommandHandler.Commands) {
            sb.AppendLine($"  {cmd.Name[0]} - {cmd.Description}");
        }

        return TextCommandResult.Success(sb.ToString());
    }
}
