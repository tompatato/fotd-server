#pragma once

#include <fom-network/Interop.h>
#include <fom-network/enums/WorldObjectUpdate.h>
#include <fom-network/types/WorldObject.h>

namespace FOMNetwork {
namespace Packet {

constexpr int MAX_WORLD_OBJECTS = 64;

// ID_WORLD_OBJECTS (133): server -> client placed-object management. A
// discriminated union keyed by `subType` (see Enum::WorldObjectUpdate). The
// server emits CATEGORY updates: one category's object vector. `count` objects
// of `objects` are valid. See knowledge-base/client/World Objects.md.
#pragma pack(push, 1)
struct WorldObjects {
  Enum::WorldObjectUpdate subType;
  uint16_t category;  // object category discriminator (0x1fa..0x204)
  uint16_t count;
  Type::WorldObject objects[MAX_WORLD_OBJECTS];
};
#pragma pack(pop)

ASSERT_BLITTABLE(WorldObjects);

}  // namespace Packet
}  // namespace FOMNetwork
