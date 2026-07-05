using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    [PacketId(PacketIdentifier.ID_MAIL)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Mail
    {
        public uint PlayerId;

        /// <summary>
        /// Number of mail entries that follow on the wire. Only the empty-inbox
        /// reply (0) is produced today, which is enough to answer the client's
        /// mail check and release its vortex gate.
        /// </summary>
        public byte MailCount;
    }
}
