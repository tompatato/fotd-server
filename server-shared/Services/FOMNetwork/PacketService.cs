using FOMServer.Shared.Models;
using System.Buffers;
using System.Runtime.InteropServices;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public class PacketService : IPacketService
	{
		private static readonly MemoryPool<FOMPacket> Pool = MemoryPool<FOMPacket>.Shared;

		/// <inheritdoc />
		public ReceivedPackets Receive(IntPtr peer)
		{
			return FOMNetwork_ReceivePackets(peer);
		}

		/// <inheritdoc />
		public void Process(IntPtr peer, ref ReceivedPackets received, Span<FOMPacket> packetBuffer)
		{
			if (packetBuffer.Length < received.count)
				throw new ArgumentException("Buffer too small");

			unsafe
			{
				fixed (FOMPacket* ptr = &MemoryMarshal.GetReference(packetBuffer))
				{
					FOMNetwork_ProcessPackets(peer, received, ptr, received.count);
				}
			}
		}

		/// <inheritdoc />
		public void Send(IntPtr peer, SendPacket[] packets)
		{
			FOMNetwork_Send(peer, packets, (uint)packets.Length);
		}

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static extern ReceivedPackets FOMNetwork_ReceivePackets(IntPtr peer);

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static unsafe extern sbyte FOMNetwork_ProcessPackets(IntPtr peer, ReceivedPackets received, FOMPacket* packetBuffer, uint packetBufferLen);

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static extern void FOMNetwork_Send(IntPtr peer, SendPacket[] packets, uint count);
	}
}
