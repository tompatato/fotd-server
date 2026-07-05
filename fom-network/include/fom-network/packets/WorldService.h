#pragma once

#include <fom-network/Interop.h>

namespace FOMNetwork {
namespace Packet {

// "World service" terminal control (id 165) — the packet the server uses to OPEN
// a terminal window on the client (market, storage, apartments, vortex, ...).
//
// TO BE REVISITED: only a minimal slice is implemented. Write emits a fixed
// "open the vortex terminal" body ({outer 5, inner 0xc} with an empty payload) so
// the Vortex Network menu shows; Read only consumes the leading discriminator so
// the client's terminal requests don't error. Populating the menu (destinations),
// selection/purchase, and the other terminals remain future work — and the real
// trigger is a placed vortex-terminal object, not the walk-in gate.
#pragma pack(push, 1)
struct WorldService {
  uint32_t playerId;
  uint8_t discriminator;  // request discriminator (client -> world); unused on write
};
#pragma pack(pop)

ASSERT_BLITTABLE(WorldService);

}  // namespace Packet
}  // namespace FOMNetwork
