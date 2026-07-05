#include <fom-network/packets/VortexGate.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

namespace {
// One reachable-destination entry (client reader FUN_1026f2e0): world id byte,
// the server address as a RakNet SystemAddress (32-bit ip stored inverted, then
// 16-bit port), and a trailing 16-bit field (cost/flags — unused for now).
void WriteDestination(RakNet::BitStream& bs, uint8_t worldId, uint32_t ip,
                      uint16_t port) {
  bs.WriteCompressed(worldId);
  uint32_t invertedIp = ~ip;
  bs.WriteBits(reinterpret_cast<const unsigned char*>(&invertedIp), 32);
  bs.WriteBits(reinterpret_cast<const unsigned char*>(&port), 16);
  bs.WriteCompressed(static_cast<uint16_t>(0));
}
}  // namespace

void VortexGateSerializer::Write(RakNet::BitStream& bs,
                                 const Packet::VortexGate* data) const {
  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(data->type);

  if (data->type == Enum::VORTEX_GATE_TYPE_LIST_DATA) {
    // PROBE: a hardcoded reachable-world list for the worlds this process hosts,
    // all reachable at the single world-server client endpoint. Enough to confirm
    // the vortex menu populates; the real list should be built from live data.
    const uint32_t ip = 0x0100007F;  // 127.0.0.1 (network-order u32)
    const uint16_t port = 61001;
    bs.WriteCompressed(static_cast<uint8_t>(2));  // destination count
    WriteDestination(bs, static_cast<uint8_t>(Enum::NYC_MANHATTAN), ip, port);
    WriteDestination(bs, static_cast<uint8_t>(Enum::APARTMENTS), ip, port);
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
