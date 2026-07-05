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

  // Only the travel request carries a world/node pair. Other sub-types have
  // different (or empty) bodies and are not handled here, so reject them rather
  // than misparse the remaining bits — this flags the packet as a read error
  // that managed code can drop gracefully.
  if (data->type != Enum::VORTEX_GATE_TYPE_TRAVEL_REQUEST) return false;
  if (!bs.ReadCompressed(data->world)) return false;
  if (!bs.ReadCompressed(data->node)) return false;

  return true;
}

}  // namespace Packet
}  // namespace FOMNetwork
