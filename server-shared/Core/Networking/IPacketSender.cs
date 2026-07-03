using FOMServer.Shared.Core.Packets.Types;

namespace FOMServer.Shared.Core.Networking
{
    public interface IPacketSender
    {
        void EnqueueSend(in QueuePacket packet);

        /// <summary>
        /// Requests that the connection to the given client be closed. The
        /// request is processed on the network thread, mirroring sends.
        /// </summary>
        void Disconnect(in NetworkAddress address);
    }
}
