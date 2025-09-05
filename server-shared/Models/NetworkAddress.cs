using System.Runtime.InteropServices;

namespace FOMServer.Shared.Models
{
	/**
	 * Represents a network address with an IP address and port number.
	 * This structure is used for specifying the source or destination of network packets.
	 */
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct NetworkAddress
	{
		public readonly uint address;
		public readonly short port;
	}
}
