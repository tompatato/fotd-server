using FOMServer.Shared.Models;
using System.Runtime.InteropServices;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public partial class NetworkService : INetworkService
	{
		/// <inheritdoc />
		public sbyte ValidatePacketStructs(PacketStructure[] structures) => FOMNetwork_ValidatePacketStructs(structures, structures.Length);

		[LibraryImport("FOMNetwork")]
		private static partial sbyte FOMNetwork_ValidatePacketStructs(PacketStructure[] structures, int count);
	}


}
