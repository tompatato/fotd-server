#pragma once

#include <fom-network/InteropTypes.h>

namespace FOMNetwork {
namespace Enum {

// Sub-type discriminator carried by ID_VORTEX_GATE (123). The vortex terminal
// multiplexes several operations onto a single packet id; only the world-travel
// request/approve pair is handled server-side today. The remaining values are
// documented for completeness:
//   2 = item list, 3 = (message-box confirm variant),
//   5 = destination-list request (client -> master),
//   6 = destination-list data   (master -> client).
enum VortexGateType : uint8_t {
  VORTEX_GATE_TYPE_INVALID = 0,
  VORTEX_GATE_TYPE_ENTER = 1,           // client -> world: gate countdown elapsed
  VORTEX_GATE_TYPE_TRAVEL_APPROVE = 4,  // world -> client: travel authorized
  VORTEX_GATE_TYPE_TRAVEL_REQUEST = 7,  // client -> world: take me to world/node
};

}  // namespace Enum
}  // namespace FOMNetwork
