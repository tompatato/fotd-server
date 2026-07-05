#pragma once

#include <fom-network/Interop.h>
#include <fom-network/enums/VortexGateType.h>
#include <fom-network/enums/WorldId.h>

namespace FOMNetwork {
namespace Packet {

#pragma pack(push, 1)
struct VortexGate {
  uint32_t playerId;
  Enum::VortexGateType type;
  Enum::WorldId world;  // destination world (travel sub-types 4/7)
  uint8_t node;         // destination node within the world (travel sub-types 4/7)

  // LIST_DATA (sub-type 6): the reachable destinations to populate the vortex
  // menu. All hosted worlds are reachable at one server endpoint today, so a
  // single shared address is carried rather than one per entry.
  uint32_t serverIp;
  uint16_t serverPort;
  uint8_t destinationCount;
  Enum::WorldId destinations[Enum::NUM_WORLDS];
};
#pragma pack(pop)

ASSERT_BLITTABLE(VortexGate);

}  // namespace Packet
}  // namespace FOMNetwork
