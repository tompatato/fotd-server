#pragma once

#include <fom-network/Interop.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Starts an interface for sending and receiving packets as a server.
 *
 * @param port The port to listen for incoming connections on.
 * @param maxClients The maximum number of clients that can connect to the
 * server.
 * @param threadSleepMs The length of time the network thread should sleep
 * between cycles.
 * @return A pointer to the initialized interface, or null on failure.
 */
FOM_API FOMNetworkPeer* FOMNetwork_Server_Startup(uint16_t port,
                                                  uint32_t maxClients,
                                                  int32_t threadSleepMs);

/**
 * Shuts down the server interface.
 *
 * @param peer The interface instance to shut down.
 */
FOM_API void FOMNetwork_Server_Shutdown(FOMNetworkPeer* server);

/**
 * Closes the connection to a single connected client, sending it a
 * disconnection notification so it can tear down its side gracefully.
 *
 * @param server The server interface instance.
 * @param binaryAddress The client's binary IPv4 address (network order).
 * @param port The client's port.
 */
FOM_API void FOMNetwork_Server_CloseConnection(FOMNetworkPeer* server,
                                               uint32_t binaryAddress,
                                               uint16_t port);

#ifdef __cplusplus
}
#endif
