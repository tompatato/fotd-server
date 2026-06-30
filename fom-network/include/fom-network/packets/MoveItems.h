#pragma once

#include <fom-network/Interop.h>
#include <fom-network/constants/BufferSizes.h>

namespace FOMNetwork {
namespace Packet {

#pragma pack(push, 1)
struct MoveItems {
  uint32_t playerId;
  uint16_t idCount;
  uint32_t ids[BufferSizes::MAX_ITEM_LIST_SIZE];
  uint8_t src;
  uint8_t dest;
  uint8_t srcSlot;
  uint8_t destSlot;
};
#pragma pack(pop)

ASSERT_BLITTABLE(MoveItems);

}  // namespace Packet
}  // namespace FOMNetwork
