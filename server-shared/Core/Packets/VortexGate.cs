using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    [PacketId(PacketIdentifier.ID_VORTEX_GATE)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VortexGate
    {
        // Capacity of the destination list; mirrors the native Enum::NUM_WORLDS array.
        public const int MaxDestinations = 33;

        public uint PlayerId;
        public VortexGateType Type;
        public WorldId World;
        public byte Node;

        // LIST_DATA (sub-type 6) fields: reachable destinations for the vortex menu.
        public uint ServerIp;
        public ushort ServerPort;
        public byte DestinationCount;
        public DestinationsArray Destinations;

        [InlineArray(MaxDestinations)]
        public struct DestinationsArray
        {
            private WorldId _element;
        }
    }
}
