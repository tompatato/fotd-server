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
    internal class MoveItemsHandler : PacketHandlerBase<MoveItems>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<MoveItemsHandler> _logger;

        public MoveItemsHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ILogger<MoveItemsHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in MoveItems p)
        {
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received item move from unregistered client '{Sender}'", sender);
                return;
            }

            if (p.PlayerId != player.Id)
            {
                _logger.LogWarning("Player {PlayerId} sent an item move for {OtherId}", player.Id, p.PlayerId);
                return;
            }

            // Minimal echo: the client only applies a move once the server confirms it,
            // so reflect the validated request back verbatim. There is no authoritative
            // inventory model yet (see RegisterClientHandler), so the move is not persisted
            // or validated against real container contents.
            using var response = new PacketWriter<MoveItems>(sender);
            ref var rData = ref response.Data;
            rData = p;
            _clientPacketSender.Send(response.Build());
        }
    }
}
