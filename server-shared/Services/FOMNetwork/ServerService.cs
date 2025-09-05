using System.Runtime.InteropServices;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public class ServerService : IServerService
	{
		/// <inheritdoc />
		public IntPtr Startup(ushort port)
		{
			return FOMNetwork_ServerStartup(port);
		}

		/// <inheritdoc />
		public void Shutdown(IntPtr server)
		{
			FOMNetwork_ServerShutdown(server);
		}

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr FOMNetwork_ServerStartup(ushort port);

		[DllImport("FOMNetwork", CallingConvention = CallingConvention.Cdecl)]
		private static extern void FOMNetwork_ServerShutdown(IntPtr server);
	}
}
