using FOMServer.Shared.Models;
using System.Runtime.InteropServices;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public class NetworkService : INetworkService
	{
		/// <inheritdoc />
		public sbyte ValidatePacketStructs(PacketStructure[] structures)
		{
			ArgumentNullException.ThrowIfNull(structures);
			return FOMNetwork_ValidatePacketStructs(structures, structures.Length);
		}

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static extern sbyte FOMNetwork_ValidatePacketStructs(PacketStructure[] structures, int count);
	}


}
