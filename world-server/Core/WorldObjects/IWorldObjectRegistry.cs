using FOMServer.Shared.Core.Packets.Types;

namespace FOMServer.World.Core.WorldObjects
{
    /// <summary>
    /// Tracks the non-player objects placed in the world (vortex terminals,
    /// storage, mining rigs, and other deployables). The authoritative source
    /// of what every client should see; new placements are broadcast and the
    /// full set is replayed to players as they enter the world.
    /// </summary>
    internal interface IWorldObjectRegistry
    {
        /// <summary>
        /// Places a new object, assigning it a unique instance id, and returns it.
        /// </summary>
        PlacedObject Add(ushort category, ushort itemType, PositionRotation position, uint extra);

        /// <summary>
        /// Returns a snapshot of every placed object.
        /// </summary>
        IReadOnlyList<PlacedObject> Snapshot();
    }
}
