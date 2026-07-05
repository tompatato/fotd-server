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
  Enum::WorldId world;  // destination world (only valid for travel sub-types)
  uint8_t node;         // destination node within the world
};
#pragma pack(pop)

ASSERT_BLITTABLE(VortexGate);

}  // namespace Packet
}  // namespace FOMNetwork
