#include <fom-network/packets/WorldObjects.h>

#include "../types/PositionRotationSerializer.h"
#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

namespace {
// One object record, matching the client element reader Object.lto FUN_100dc250:
// id (u32), type (u16), state (u8), extra (u32), then the PositionRotation
// (precision 16 — compressed u16 per axis + 9-bit yaw). Every scalar is
// WriteCompressed.
void WriteObject(RakNet::BitStream& bs, const Type::WorldObject& obj) {
  static const Type::PositionRotationSerializer positionRotationSerializer;

  bs.WriteCompressed(obj.id);
  bs.WriteCompressed(obj.type);
  bs.WriteCompressed(obj.state);
  bs.WriteCompressed(obj.extra);
  positionRotationSerializer.Write(bs, obj.position);
}
}  // namespace

void WorldObjectsSerializer::Write(RakNet::BitStream& bs,
                                   const Packet::WorldObjects* data) const {
  bs.WriteCompressed(data->subType);

  // Only the CATEGORY arm is emitted today: a single category's object vector.
  // Its wire body is the category id then a compressed u32 count followed by
  // that many records.
  bs.WriteCompressed(data->category);

  uint16_t count = data->count;
  if (count > MAX_WORLD_OBJECTS) count = MAX_WORLD_OBJECTS;

  bs.WriteCompressed(static_cast<uint32_t>(count));
  for (uint16_t i = 0; i < count; ++i) WriteObject(bs, data->objects[i]);
}

}  // namespace Packet
}  // namespace FOMNetwork
