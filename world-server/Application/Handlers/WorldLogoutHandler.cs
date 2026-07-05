using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.Handlers
{
    [PacketHandler]
    internal class WorldLogoutHandler : PacketHandlerBase<WorldLogout>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<WorldLogoutHandler> _logger;

        public WorldLogoutHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ILogger<WorldLogoutHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in WorldLogout p)
        {
            var player = _playerRegistry.Get(p.PlayerId);
            if (player is null)
            {
                _logger.LogWarning("Received world logout for player {PlayerId} not in this world", p.PlayerId);
                return;
            }

            // Closing the client's world connection is the signal it waits on to
            // leave the "Logging Out" screen and return to character select.
            _clientPacketSender.Disconnect(player.Address);
            _playerRegistry.Logout(player);
        }
    }
}
