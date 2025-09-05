using FOMServer.Shared.Models;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public interface INetworkService
	{
		/// <summary>
		/// Validates an array of given packet structures against the network
		/// library's expected packet structures.
		/// </summary>
		/// <param name="structures">The structures to validate.</param>
		/// <returns>When successful, 0, otherwise:
		/// - -1: Library did not receive the number of structs expected.
		/// - -2: Library was asked to validate a struct that does not exist.
		/// - -3: Library received a struct with an unexpected size.
		/// </returns>
		sbyte ValidatePacketStructs(PacketStructure[] structures);
	}
}
