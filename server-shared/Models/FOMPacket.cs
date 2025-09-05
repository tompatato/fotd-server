
using FOMServer.Shared.Enums;
using FOMServer.Shared.Packets;
using System.Runtime.InteropServices;

/**
 * This file contains all of the packet structures defined in `fom-network/include/fom-network/FOMPacket.h`.
 * 
 * In order for the interop to work correctly and efficiently, ALL of them must:
 * 
 * - Match the C++ structure's data type sizes and layout EXACTLY.
 * - Use only blittable types (no bools, no strings, no arrays, no reference types)
 * - Be marked with `[StructLayout(LayoutKind.Sequential, Pack = 1)]` to ensure no padding is added.
 * - Use only `readonly` fields to ensure immutability.
 */
namespace FOMServer.Shared.Models
{
	/**
	 * The main packet structure that encapsulates all packet types.
	 * This structure is used for sending and receiving packets over the network.
	 */
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FOMPacket
	{
		/**
		 * A union of all possible packet data types.
		 * 
		 * This replicates the structure of the FOMPacket union in C++.
		 * Make sure to always use `[FieldOffset(0)]` to ensure that
		 * their memory overlaps the same as way as a union in C++.
		 */
		[StructLayout(LayoutKind.Explicit, Pack = 1)]
		public struct FOMPacketData
		{
			[FieldOffset(0)] public FOMPacketError error;
			[FieldOffset(0)] public ExamplePacket example;
		}

		public readonly PacketIdentifier ID;
		public readonly NetworkAddress sender;
		public readonly FOMPacketData data;
	}
}
