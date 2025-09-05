using FOMServer.Shared.Models;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public interface INetworkService
	{
		/// <summary>
		/// Validates the FOMPacket structures against the network
		/// library's expected packet structures.
		/// </summary>
		/// <remarks>
		/// This is an important step in ensuring that the structs are
		/// blittable and correctly defined for interop. Doing so
		/// makes sure our structs are zero-copy and maximizes
		/// their performance.
		/// </remarks>
		/// <exception>
		/// Throws if the structs do not match the network library's expectations.
		/// </exception>
		void ValidateFOMPacket();
	}
}
