#pragma once

#include <fom-network/Interop.h>
#include <fom-network/types/PositionRotation.h>

namespace FOMNetwork {
namespace Type {

// One placed world object, as decoded by the client element reader
// Object.lto FUN_100dc250 (28-byte record). Wire order is id, type, state,
// extra, position — note position lives at memory offset 0x08 but is written
// last. `type` is an ItemType that indexes g_ItemDefTable for the model.
#pragma pack(push, 1)
struct WorldObject {
  uint32_t id;         // server-assigned instance id
  uint16_t type;       // ItemType -> model (ItemDefinition + 0x10)
  uint8_t state;       // state / variant byte
  uint32_t extra;      // owner / faction / context
  PositionRotation position;  // placement (precision 16)
};
#pragma pack(pop)

ASSERT_BLITTABLE(WorldObject);

}  // namespace Type
}  // namespace FOMNetwork
