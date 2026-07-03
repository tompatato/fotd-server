#pragma once

#include <fom-network/Interop.h>

namespace FOMNetwork {
namespace Packet {

#pragma pack(push, 1)
struct WorldLogout {
  uint32_t playerId;
  uint8_t isChangingWorlds;
};
#pragma pack(pop)

ASSERT_BLITTABLE(WorldLogout);

}  // namespace Packet
}  // namespace FOMNetwork
