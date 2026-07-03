using FOMServer.Master.Core.Networking;
using FOMServer.Master.Core.Players;
using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;

namespace FOMServer.Master.Application.Handlers
{
    [PacketHandler]
    internal class WorldLogoutHandler : PacketHandlerBase<WorldLogout>
    {
        private readonly IWorldPacketSender _worldPacketSender;
        private readonly IClientRegistry _clientRegistry;
        private readonly IWorldServerRegistry _worldServerRegistry;
        private readonly ILogger<WorldLogoutHandler> _logger;

        public WorldLogoutHandler(
            IWorldPacketSender worldPacketSender,
            IClientRegistry clientRegistry,
            IWorldServerRegistry worldServerRegistry,
            ILogger<WorldLogoutHandler> logger)
        {
            _worldPacketSender = worldPacketSender;
            _clientRegistry = clientRegistry;
            _worldServerRegistry = worldServerRegistry;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in WorldLogout p)
        {
            var session = _clientRegistry.Get(sender);
            if (session is null)
            {
                _logger.LogWarning("Dropping unexpected world logout from '{Sender}'", sender);
                return;
            }

            if (session.PlayerId != p.PlayerId)
            {
                _logger.LogWarning(
                    "Player {PlayerId} attempted world logout on session belonging to {SessionPlayerId}",
                    p.PlayerId,
                    session.PlayerId);
                return;
            }

            // A world switch is driven by the client's subsequent ID_WORLD_LOGIN,
            // which the WorldLoginHandler already orchestrates; this notification
            // needs no action of its own.
            if (p.IsChangingWorlds)
            {
                return;
            }

            if (!session.CurrentWorld.HasValue)
            {
                // The player isn't in a world (or already left) — nothing to end.
                return;
            }

            var worldId = session.CurrentWorld.Value;
            var worldServer = _worldServerRegistry.Get(worldId);

            // The client stays connected to the master and returns to
            // character-select; it is no longer in any world.
            session.LeaveWorld();

            if (worldServer is null)
            {
                // The world server is gone, so the client's world connection is
                // already dead — the logout is effectively complete.
                return;
            }

            // Hand the logout to the world server so it can end the session and
            // close the client's world connection (the client's cue that logout
            // succeeded).
            using var forward = new PacketWriter<WorldLogout>(worldServer.ServerAddress);
            forward.Data = p;
            _worldPacketSender.Send(forward.Build());
        }
    }
}
