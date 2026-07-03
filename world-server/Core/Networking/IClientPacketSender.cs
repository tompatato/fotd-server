using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets.Types;

namespace FOMServer.World.Core.Networking
{
    internal interface IClientPacketSender
    {
        void Send(in QueuePacket packet);

        void Broadcast(in QueuePacket packet);

        /// <summary>
        /// Closes a client's connection, prompting it to tear down its world
        /// session (used to complete a logout).
        /// </summary>
        void Disconnect(in NetworkAddress address);
    }
}
