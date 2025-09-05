using System.Runtime.InteropServices;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public class ClientService : IClientService
	{
		/// <inheritdoc />
		public IntPtr Connect(string hostAddress, ushort port)
		{
			return FOMNetwork_Client_Connect(hostAddress, port);
		}

		/// <inheritdoc />
		public void Disconnect(IntPtr client)
		{
			FOMNetwork_Client_Disconnect(client);
		}

		[DllImport("FOMNetwork",
			CallingConvention = CallingConvention.Cdecl,
			CharSet = CharSet.Ansi)]
		private static extern IntPtr FOMNetwork_Client_Connect(string hostAddress, ushort port);

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static extern void FOMNetwork_Client_Disconnect(IntPtr client);
	}
}
