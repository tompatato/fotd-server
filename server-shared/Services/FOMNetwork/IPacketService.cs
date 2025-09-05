using FOMServer.Shared.Models;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public interface IPacketService
	{
		/// <summary>
		/// Polls the network interface for packets and returns them in a buffer.
		/// </summary>
		/// <param name="peer">The network peer to read.</param>
		/// <returns>A structure containing the library's packet buffer and the number of packets.</returns>
		ReceivedPackets Receive(IntPtr peer);

		/// <summary>
		/// Uses the received packets to fill a buffer with deserialized FOMPacket structures.
		/// </summary>
		/// <param name="peer">The network peer to read.</param>
		/// <param name="received">The packets received from a call to Receive().</param>
		/// <param name="packets">A buffer for the number of FOMPacket instances received by the library.</param>
		void Process(IntPtr peer, ref ReceivedPackets received, ref FOMPacket[] packets);

		/// <summary>
		/// Sends packets to the specified destinations.
		/// </summary>
		/// <param name="peer">The peer to send packets using.</param>
		/// <param name="packets">An array of packets to send.</param>
		void Send(IntPtr peer, ref SendPacket[] packets);
	}
}
