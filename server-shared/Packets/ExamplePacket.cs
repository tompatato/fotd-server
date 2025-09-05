using System.Runtime.InteropServices;

namespace FOMServer.Shared.Packets
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ExamplePacket
	{
		public readonly int exampleField1;
	}
}
