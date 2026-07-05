using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Core.WorldObjects
{
    /// <summary>
    /// Places world objects and syncs them to clients via
    /// <c>ID_WORLD_OBJECTS</c>. The placement counterpart to the player-update
    /// relay: deployments are broadcast to everyone, and the current set is
    /// replayed to players as they enter the world.
    /// </summary>
    internal interface IWorldObjectService
    {
        /// <summary>
        /// Places an object of the given item type at the player's current
        /// position and broadcasts it to all clients.
        /// </summary>
        PlacedObject Deploy(Player player, ushort itemType);

        /// <summary>
        /// Sends every already-placed object to a single client (world entry).
        /// </summary>
        void SendExistingTo(in NetworkAddress address);
    }
}
