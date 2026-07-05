#include <fom-network/constants/BufferSizes.h>
#include <fom-network/packets/MoveItems.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

bool MoveItemsSerializer::Read(RakNet::BitStream& bs,
                               Packet::MoveItems* data) const {
  if (!bs.ReadCompressed(data->playerId)) return false;

  uint16_t idCount;
  if (!bs.ReadCompressed(idCount)) return false;
  if (idCount > BufferSizes::MAX_ITEM_LIST_SIZE) return false;
  data->idCount = idCount;
  for (uint16_t i = 0; i < idCount; ++i) {
    if (!bs.ReadCompressed(data->ids[i])) return false;
  }

  if (!bs.ReadCompressed(data->src)) return false;
  if (!bs.ReadCompressed(data->dest)) return false;
  if (!bs.ReadCompressed(data->srcSlot)) return false;
  if (!bs.ReadCompressed(data->destSlot)) return false;

  return true;
}

void MoveItemsSerializer::Write(RakNet::BitStream& bs,
                                const Packet::MoveItems* data) const {
  uint16_t idCount = data->idCount;
  if (idCount > BufferSizes::MAX_ITEM_LIST_SIZE)
    idCount = BufferSizes::MAX_ITEM_LIST_SIZE;

  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(idCount);
  for (uint16_t i = 0; i < idCount; ++i) bs.WriteCompressed(data->ids[i]);

  bs.WriteCompressed(data->src);
  bs.WriteCompressed(data->dest);
  bs.WriteCompressed(data->srcSlot);
  bs.WriteCompressed(data->destSlot);
}

}  // namespace Packet
}  // namespace FOMNetwork
