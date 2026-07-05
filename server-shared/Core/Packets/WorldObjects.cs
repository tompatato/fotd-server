using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    /// <summary>
    /// ID_WORLD_OBJECTS (133): server to client placed-object management. A
    /// discriminated union keyed by <see cref="SubType"/>; the server emits
    /// <see cref="WorldObjectUpdate.Category"/> updates carrying one category's
    /// object vector (<see cref="Count"/> valid <see cref="Objects"/>).
    /// See knowledge-base/client/World Objects.md.
    /// </summary>
    [PacketId(PacketIdentifier.ID_WORLD_OBJECTS)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldObjects
    {
        public const int MaxWorldObjects = 64;

        public WorldObjectUpdate SubType;
        public ushort Category;
        public ushort Count;
        public ObjectsArray Objects;

        [InlineArray(MaxWorldObjects)]
        public struct ObjectsArray
        {
            private WorldObject _element;
        }
    }
}
