using System.Runtime.InteropServices;

namespace FOMServer.Shared.Services.FOMNetwork
{
	public partial class ServerService : IServerService
	{
		/// <inheritdoc />
		public IntPtr Startup(ushort port) => FOMNetwork_ServerStartup(port);

		/// <inheritdoc />
		public void Shutdown(IntPtr server) => FOMNetwork_ServerShutdown(server);

		[LibraryImport("FOMNetwork")]
		private static partial IntPtr FOMNetwork_ServerStartup(ushort port);

		[LibraryImport("FOMNetwork")]
		private static partial void FOMNetwork_ServerShutdown(IntPtr server);
	}
}
