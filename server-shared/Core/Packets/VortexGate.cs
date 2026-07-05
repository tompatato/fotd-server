using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    [PacketId(PacketIdentifier.ID_VORTEX_GATE)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VortexGate
    {
        public uint PlayerId;
        public VortexGateType Type;
        public WorldId World;
        public byte Node;
    }
}
