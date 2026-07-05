using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.Handlers
{
    /// <summary>
    /// Receives the client's world-service terminal requests (id 165).
    /// </summary>
    /// <remarks>
    /// TO BE REVISITED: still a thin slice. When the vortex terminal is shown it
    /// sends discriminator 0x12; we answer with the vortex destination list so the
    /// menu populates. Other discriminators (node selection, purchase) and the real
    /// destination data are not handled yet, and the menu is opened provisionally
    /// from the walk-in gate rather than a placed terminal object.
    /// </remarks>
    [PacketHandler]
    internal class WorldServiceHandler : PacketHandlerBase<WorldService>
    {
        // Sent by the vortex terminal's shown event (FUN_101c2010).
        private const byte VortexTerminalShown = 0x12;

        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<WorldServiceHandler> _logger;

        public WorldServiceHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ILogger<WorldServiceHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in WorldService p)
        {
            if (p.Discriminator != VortexTerminalShown)
            {
                _logger.LogDebug("Unhandled world service request (disc {Discriminator}) from player {PlayerId}", p.Discriminator, p.PlayerId);
                return;
            }

            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("World service request from unregistered client '{Sender}'", sender);
                return;
            }

            // Answer the just-shown vortex terminal with its destination list so the
            // menu populates. The list contents are currently hardcoded in the
            // native serializer (see VortexGateSerializer, LIST_DATA arm).
            using var list = new PacketWriter<VortexGate>(sender);
            ref var rData = ref list.Data;
            rData.PlayerId = player.Id;
            rData.Type = VortexGateType.ListData;
            _clientPacketSender.Send(list.Build());

            _logger.LogInformation("Sent vortex destination list to player {PlayerId}", player.Id);
        }
    }
}
