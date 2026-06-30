using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Constants;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    [PacketId(PacketIdentifier.ID_MOVE_ITEMS)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MoveItems
    {
        public uint PlayerId;
        public ushort IdCount;
        public IdArray Ids;
        public byte Src;
        public byte Dest;
        public byte SrcSlot;
        public byte DestSlot;

        [InlineArray(BufferSizes.MaxItemListSize)]
        public struct IdArray
        {
            private uint _id;
        }
    }
}
