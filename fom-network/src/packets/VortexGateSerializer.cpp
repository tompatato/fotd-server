#include <fom-network/packets/VortexGate.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

void VortexGateSerializer::Write(RakNet::BitStream& bs,
                                 const Packet::VortexGate* data) const {
  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(data->type);
  bs.WriteCompressed(data->world);
  bs.WriteCompressed(data->node);
}

bool VortexGateSerializer::Read(RakNet::BitStream& bs,
                                Packet::VortexGate* data) const {
  if (!bs.ReadCompressed(data->playerId)) return false;
  if (!bs.ReadCompressed(data->type)) return false;

  // The travel request/approve arms carry a world/node pair; other sub-types
  // have different (or empty) bodies we don't model. Read the pair only for the
  // travel arms and accept the rest without consuming their payload, so the
  // packet still reaches the handler (which decides what to act on) instead of
  // being dropped as a read error.
  data->world = Enum::MASTER_SERVER;
  data->node = 0;
  if (data->type == Enum::VORTEX_GATE_TYPE_TRAVEL_REQUEST ||
      data->type == Enum::VORTEX_GATE_TYPE_TRAVEL_APPROVE) {
    if (!bs.ReadCompressed(data->world)) return false;
    if (!bs.ReadCompressed(data->node)) return false;
  }

  return true;
}

}  // namespace Packet
}  // namespace FOMNetwork
