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
    /// Answers the client's on-entry mail check (<see cref="CheckMail"/>) with an
    /// empty <see cref="Mail"/> reply. Until this reply arrives the client sits in
    /// a "checking for new mail" state that blocks vortex travel, so the handshake
    /// must complete even though no mail feature exists yet.
    /// </summary>
    [PacketHandler]
    internal class CheckMailHandler : PacketHandlerBase<CheckMail>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<CheckMailHandler> _logger;

        public CheckMailHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ILogger<CheckMailHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in CheckMail p)
        {
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received mail check from unregistered client '{Sender}'", sender);
                return;
            }

            // Reply for the registered player rather than trusting the request's
            // id, then report an empty inbox to clear the client's mail gate.
            using var response = new PacketWriter<Mail>(sender);
            ref var rData = ref response.Data;
            rData.PlayerId = player.Id;
            rData.MailCount = 0;
            _clientPacketSender.Send(response.Build());

            // The client polls mail periodically, so keep this at debug to avoid noise.
            _logger.LogDebug("Answered mail check for player {PlayerId} with an empty inbox", player.Id);
        }
    }
}
