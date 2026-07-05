using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.Handlers
{
    /// <summary>
    /// Handles a vortex world-travel request (<see cref="VortexGateType.TravelRequest"/>).
    /// The client only stages the destination and begins the world handoff once the
    /// server approves, so this replies with <see cref="VortexGateType.TravelApprove"/>.
    /// </summary>
    /// <remarks>
    /// Single-server simplification: the approve authorises travel to the requested
    /// world only if this process hosts it, otherwise it redirects to the primary
    /// hosted world (the client obeys the world in the approve packet). This keeps
    /// travel working without a multi-world deployment while still allowing genuine
    /// travel between the worlds one server hosts. The subsequent transfer is the
    /// ordinary world-login handoff. The chosen node is echoed for the client to
    /// stage, but spawn placement is still the hard-coded node in
    /// <c>RegisterClientHandler</c> until node persistence is threaded through the
    /// handoff.
    /// </remarks>
    [PacketHandler]
    internal class VortexGateHandler : PacketHandlerBase<VortexGate>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ServerSettings _serverSettings;
        private readonly ILogger<VortexGateHandler> _logger;

        public VortexGateHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ServerSettings serverSettings,
            ILogger<VortexGateHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _serverSettings = serverSettings;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in VortexGate p)
        {
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received vortex request from unregistered client '{Sender}'", sender);
                return;
            }

            if (p.PlayerId != player.Id)
            {
                _logger.LogWarning("Player {PlayerId} sent a vortex request for {OtherId}", player.Id, p.PlayerId);
                return;
            }

            // Only the travel request reaches this handler (the serializer rejects
            // the other sub-types), but guard anyway so an unexpected type is a
            // no-op rather than a spurious approval.
            if (p.Type != VortexGateType.TravelRequest)
            {
                _logger.LogWarning("Player {PlayerId} sent unsupported vortex sub-type {Type}", player.Id, p.Type);
                return;
            }

            // Honour the requested world if this server actually hosts it (so
            // travel between the worlds this process serves works for real);
            // otherwise fall back to the primary world so single-server travel
            // still succeeds instead of stalling on an offline destination.
            var destination = Array.IndexOf(_serverSettings.WorldIds, p.World) >= 0
                ? p.World
                : _serverSettings.WorldIds[0];
            if (p.World != destination)
            {
                _logger.LogInformation(
                    "Player {PlayerId} requested vortex to {Requested}; redirecting to hosted world {Destination}",
                    player.Id, p.World, destination);
            }

            using var response = new PacketWriter<VortexGate>(sender);
            ref var rData = ref response.Data;
            rData.PlayerId = p.PlayerId;
            rData.Type = VortexGateType.TravelApprove;
            rData.World = destination;
            rData.Node = p.Node;
            _clientPacketSender.Send(response.Build());
        }
    }
}
