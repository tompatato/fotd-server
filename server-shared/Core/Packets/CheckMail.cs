using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    [PacketId(PacketIdentifier.ID_CHECK_MAIL)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CheckMail
    {
        public uint PlayerId;
    }
}
