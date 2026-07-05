#include <fom-network/packets/VortexGate.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

namespace {
// One reachable-destination entry, matching the client reader FUN_1026f2e0:
//   worldId : compressed u8
//   ip      : 32 bits, stored inverted (RakNet SystemAddress)
//   port    : 16 bits
//   extra   : compressed u16 (cost/flags — unused)
//   then two count-prefixed nested lists the client reads per entry
//   (FUN_100a9680 and FUN_100a6390 — grid/territory data). Writing their
//   counts as 0 leaves the entry well-formed with no nested rows; omitting
//   them entirely makes the client overrun and corrupt its destination table.
void WriteDestination(RakNet::BitStream& bs, uint8_t worldId, uint32_t ip,
                      uint16_t port) {
  bs.WriteCompressed(worldId);
  uint32_t invertedIp = ~ip;
  bs.WriteBits(reinterpret_cast<const unsigned char*>(&invertedIp), 32);
  bs.WriteBits(reinterpret_cast<const unsigned char*>(&port), 16);
  bs.WriteCompressed(static_cast<uint16_t>(0));  // extra

  // FUN_100a9680: leading byte + u32 row count (0 => no rows).
  bs.WriteCompressed(static_cast<uint8_t>(0));
  bs.WriteCompressed(static_cast<uint32_t>(0));

  // FUN_100a6390: leading byte + u32 + u32 row count (0 => no rows).
  bs.WriteCompressed(static_cast<uint8_t>(0));
  bs.WriteCompressed(static_cast<uint32_t>(0));
  bs.WriteCompressed(static_cast<uint32_t>(0));
}
}  // namespace

void VortexGateSerializer::Write(RakNet::BitStream& bs,
                                 const Packet::VortexGate* data) const {
  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(data->type);

  if (data->type == Enum::VORTEX_GATE_TYPE_LIST_DATA) {
    // The reachable-destination list the client reads into the vortex menu. All
    // listed worlds share one server endpoint (single-server hosting today).
    bs.WriteCompressed(data->destinationCount);
    for (uint8_t i = 0; i < data->destinationCount; ++i) {
      WriteDestination(bs, static_cast<uint8_t>(data->destinations[i]),
                       data->serverIp, data->serverPort);
    }
    // Trailing block the client reads after the list (player/grid context).
    bs.WriteCompressed(static_cast<uint32_t>(0));
    bs.WriteCompressed(static_cast<uint32_t>(0));
    bs.WriteCompressed(static_cast<uint32_t>(0));
    return;
  }

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
