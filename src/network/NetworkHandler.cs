using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace livemap.network;

public sealed class NetworkHandler : IDisposable {
    private readonly ColormapReceiver _colormapReceiver;
    private readonly LiveMap _server;
    private IServerNetworkChannel? _channel;

    public NetworkHandler(LiveMap server) {
        _server = server;
        _colormapReceiver = new ColormapReceiver(server);
        RegisterChannel();
    }

    private void RegisterChannel() {
        _channel = _server.Sapi.Network.RegisterChannel(_server.ModId)
            .RegisterMessageType<ColormapPacket>()
            .RegisterMessageType<ColormapChunkPacket>()
            .SetMessageHandler<ColormapPacket>(ReceiveColormap)
            .SetMessageHandler<ColormapChunkPacket>(_colormapReceiver.ReceiveChunk);
    }

    public void SendPacket<T>(T packet, IPlayer? receiver = null) => _channel?.SendPacket(packet, receiver as IServerPlayer);

    private void ReceiveColormap(IServerPlayer player, ColormapPacket packet) {
        if (!player.HasPrivilege(Privilege.root)) {
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.error.no-privilege".ToLang(), EnumChatType.CommandError);
            Logger.Warn("colormap.non-privileged".ToLang(player.PlayerName));
            return;
        }

        if (string.IsNullOrEmpty(packet.RawBase64String)) {
            player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.empty".ToLang(), EnumChatType.CommandError);
            Logger.Warn("colormap.empty".ToLang(player.PlayerName));
            return;
        }

        player.SendMessage(GlobalConstants.CurrentChatGroup, "command.colormap.received".ToLang(), EnumChatType.CommandSuccess);
        Logger.Info("colormap.received".ToLang(player.PlayerName));
        _server.Colormap.LoadFromPacket(_server.Sapi.World, packet);
    }

    public void Dispose() {
        _colormapReceiver.Dispose();
        _channel = null;
    }
}
