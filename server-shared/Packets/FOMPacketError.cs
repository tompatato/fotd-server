using FOMServer.Shared.Enums;
using System.Runtime.InteropServices;

namespace FOMServer.Shared.Packets
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FOMPacketError
	{
		/**
		 * The error code from the packet.
		 * 
		 * This needs to match the `FOMPacketErrorCode` enum in `fom-network/include/fom-network/FOMPacket.h`.
		 */
		public enum FOMPacketErrorCode : byte
		{
			ERROR_MISSING_PACKET_ID,
			ERROR_UNHANDLED_PACKET_ID,
			ERROR_DESERIALIZATION
		}

		public readonly PacketIdentifier offendingID;
		public readonly FOMPacketErrorCode errorCode;
	}
}
