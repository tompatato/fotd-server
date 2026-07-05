using System.Runtime.InteropServices;
using FOMServer.Shared.Infrastructure.FOMNetwork;

namespace FOMServer.Shared.Services.FOMNetwork
{
    internal partial class ServerService : IServerService
    {
        public IntPtr Startup(ushort port, uint maxClients, int threadSleepMs)
        {
            return FOMNetwork_Server_Startup(port, maxClients, threadSleepMs);
        }

        public void Shutdown(IntPtr server)
        {
            FOMNetwork_Server_Shutdown(server);
        }

        public void CloseConnection(IntPtr server, uint binaryAddress, ushort port)
        {
            FOMNetwork_Server_CloseConnection(server, binaryAddress, port);
        }

        [LibraryImport("FOMNetwork")]
        private static partial IntPtr FOMNetwork_Server_Startup(ushort port, uint maxClients, int threadSleepMs);

        [LibraryImport("FOMNetwork")]
        private static partial void FOMNetwork_Server_Shutdown(IntPtr server);

        [LibraryImport("FOMNetwork")]
        private static partial void FOMNetwork_Server_CloseConnection(IntPtr server, uint binaryAddress, ushort port);
    }
}
