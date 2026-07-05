using FOMServer.Shared.Core.Packets.Types;

namespace FOMServer.World.Core.WorldObjects
{
    /// <summary>
    /// A world object that has been placed in the world, paired with the
    /// category it belongs to (the <c>ID_WORLD_OBJECTS</c> discriminator).
    /// </summary>
    internal readonly record struct PlacedObject(ushort Category, WorldObject Object);
}
