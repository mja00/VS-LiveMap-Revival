using livemap.command.subcommand;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace livemap.command;

public class CommandHandler {
    private readonly LiveMap _server;
    private readonly IChatCommand _chatCommand;
    private readonly List<AbstractCommand> _commands = [];
    public IEnumerable<AbstractCommand> Commands => _commands;

    public CommandHandler(LiveMap server) {
        _server = server;

        _chatCommand = server.Sapi.ChatCommands
            .Create(server.ModId)
            .WithDescription("command.description".ToLang())
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(_ => "no-args-response".CommandSuccess(server.Config.Web.Url));

        RegisterSubCommand(new HelpCmd(server));
        RegisterSubCommand(new QueueCmd(server));
        RegisterSubCommand(new ColormapCmd(server));
        RegisterSubCommand(new FullRenderCmd(server));
        RegisterSubCommand(new ApothemRenderCmd(server));
        RegisterSubCommand(new ReloadCmd(server));
        RegisterSubCommand(new StatusCmd(server));
    }

    private void RegisterSubCommand(AbstractCommand command) {
        _commands.Add(command);

        _chatCommand
            .BeginSubCommands(command.Name)
            .WithDescription(command.Description)
            .WithArgs(command.ArgParsers)
            .HandleWith(args => {
                if (!args.Caller.HasPrivilege(command.Privilege)) {
                    return "error.no-privilege".CommandError();
                }

                if (command.RequiresPlayer && args.Caller.Player == null) {
                    return "error.player-only-command".CommandError();
                }

                try {
                    return command.Execute(args);
                }
                catch (Exception e) {
                    return TextCommandResult.Error(e.Message);
                }
            })
            .EndSubCommand();
    }

    public void Dispose() {
        _server.Sapi.ChatCommands.UnregisterCommand(_server.ModId);
    }
}
