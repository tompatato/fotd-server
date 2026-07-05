using FOMServer.Shared.Core.Constants;
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
    /// Receives the client's world-service terminal requests (id 165).
    /// </summary>
    /// <remarks>
    /// TO BE REVISITED: still a thin slice. When the vortex terminal is shown it
    /// sends discriminator 0x12; we answer with the vortex destination list so the
    /// menu populates. The menu is opened provisionally from the walk-in gate rather
    /// than a placed terminal object, and node-level data is not sent yet.
    /// </remarks>
    [PacketHandler]
    internal class WorldServiceHandler : PacketHandlerBase<WorldService>
    {
        // Sent by the vortex terminal's shown event (FUN_101c2010).
        private const byte VortexTerminalShown = 0x12;

        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ServerSettings _serverSettings;
        private readonly ILogger<WorldServiceHandler> _logger;

        public WorldServiceHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ServerSettings serverSettings,
            ILogger<WorldServiceHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _serverSettings = serverSettings;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in WorldService p)
        {
            if (p.Discriminator != VortexTerminalShown)
            {
                _logger.LogDebug("Unhandled world service request (disc {Discriminator}) from player {PlayerId}", p.Discriminator, p.PlayerId);
                return;
            }

            // Identify the requesting player from their authenticated connection so
            // the destination list can be scoped to them — some worlds/nodes are
            // faction/rank restricted (see the access filter below).
            //
            // The terminal is often shown before the client finishes re-registering
            // after a world transfer, so the player may not be known yet in that
            // brief window; still answer (rather than drop the request and leave the
            // menu half-populated), just without access filtering.
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogDebug(
                    "Vortex list requested before player {PlayerId} finished registering; sending unfiltered", p.PlayerId);
            }

            var serverAddress = new NetworkAddress { Address = _serverSettings.ClientIp! };
            var serverPort = ServerConstants.GetWorldClientPort(_serverSettings.WorldIds[0]);

            using var list = new PacketWriter<VortexGate>(sender);
            ref var rData = ref list.Data;
            rData.PlayerId = p.PlayerId;
            rData.Type = VortexGateType.ListData;
            rData.ServerIp = serverAddress.BinaryAddress;
            rData.ServerPort = serverPort;

            byte count = 0;
            foreach (var world in _serverSettings.WorldIds)
            {
                if (count >= VortexGate.MaxDestinations)
                {
                    break;
                }

                // TODO: faction/rank access control. Some worlds (and their nodes)
                // are only reachable by certain factions or account types; once the
                // world server carries that data, skip worlds this `player` may not
                // travel to (and, when `player` is null, restrict to unrestricted
                // worlds only). For now every hosted world is offered.
                if (!CanTravelTo(player, world))
                {
                    continue;
                }

                rData.Destinations[count] = world;
                count++;
            }

            rData.DestinationCount = count;
            _clientPacketSender.Send(list.Build());

            _logger.LogDebug(
                "Sent vortex destination list ({Count} worlds) to player {PlayerId}", count, p.PlayerId);
        }

        // Placeholder for per-player destination access. Everything is reachable
        // until faction/rank/ticket rules and their data exist on the world server.
        private static bool CanTravelTo(Player? player, WorldId world) => true;
    }
}
