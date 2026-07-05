using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    // Probe: only carries PlayerId; the serializer writes a fixed "open the
    // vortex terminal" body. See the native WorldService.h / serializer.
    [PacketId(PacketIdentifier.ID_WORLDSERVICE)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldService
    {
        public uint PlayerId;

        /// <summary>Request discriminator from the client; unused when the server writes.</summary>
        public byte Discriminator;
    }
}
