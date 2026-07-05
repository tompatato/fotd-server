#pragma once

#include <fom-network/InteropTypes.h>

namespace FOMNetwork {
namespace Enum {

// Sub-type discriminator carried by ID_WORLD_OBJECTS (133). The packet
// multiplexes several object operations onto one id (client reader
// Object.lto FUN_100568d0). The server only emits SNAPSHOT / CATEGORY today.
//   3 = set a state flag on a live object, 4 = per-object detail update.
enum WorldObjectUpdate : uint8_t {
  WORLD_OBJECT_UPDATE_INVALID = 0,
  WORLD_OBJECT_UPDATE_SNAPSHOT = 1,  // all categories at once
  WORLD_OBJECT_UPDATE_CATEGORY = 2,  // one category's object vector
  WORLD_OBJECT_UPDATE_STATE = 3,     // set a flag on an existing object
  WORLD_OBJECT_UPDATE_DETAIL = 4,    // per-object detail update
};

}  // namespace Enum
}  // namespace FOMNetwork
