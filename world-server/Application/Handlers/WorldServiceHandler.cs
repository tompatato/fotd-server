using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;

namespace FOMServer.World.Application.Handlers
{
    /// <summary>
    /// Receives the client's world-service terminal requests (id 165).
    /// </summary>
    /// <remarks>
    /// TO BE REVISITED: this is only a stub so the vortex terminal's requests
    /// (open acknowledgement, destination list, node selection, purchase) don't
    /// surface as read errors. None of them are acted on yet — the vortex menu is
    /// opened provisionally from the walk-in gate (see <see cref="VortexGateHandler"/>)
    /// and is not wired to real terminal placements or destination population.
    /// </remarks>
    [PacketHandler]
    internal class WorldServiceHandler : PacketHandlerBase<WorldService>
    {
        private readonly ILogger<WorldServiceHandler> _logger;

        public WorldServiceHandler(ILogger<WorldServiceHandler> logger)
        {
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in WorldService p)
        {
            _logger.LogDebug("Received unhandled world service request from player {PlayerId}", p.PlayerId);
        }
    }
}
