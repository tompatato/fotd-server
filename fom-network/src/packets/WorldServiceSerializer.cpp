#include <fom-network/packets/WorldService.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

namespace {
// Each entry in the two terminal-context lists begins with a presence bit; a
// false bit means "empty entry" and consumes nothing else. Ten fixed slots.
void WriteEmptyEntryList(RakNet::BitStream& bs) {
  bs.WriteCompressed(static_cast<uint32_t>(0));  // list header uint
  for (int i = 0; i < 10; ++i) {
    bs.Write(false);
  }
}
}  // namespace

void WorldServiceSerializer::Write(RakNet::BitStream& bs,
                                   const Packet::WorldService* data) const {
  const uint8_t emptyString[1] = {0};

  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(static_cast<uint8_t>(5));  // outer discriminator: open terminal

  // Open-terminal body (client reader FUN_100d4620). Vortex is inner 0x0c.
  bs.WriteCompressed(static_cast<uint32_t>(0));    // context id
  bs.WriteCompressed(static_cast<uint8_t>(0x0c));  // inner discriminator: vortex terminal
  bs.WriteCompressed(static_cast<uint8_t>(0));
  bs.WriteCompressed(static_cast<uint8_t>(0));
  bs.WriteCompressed(static_cast<uint32_t>(0));
  bs.WriteCompressed(static_cast<uint32_t>(0));
  EncodeString(bs, emptyString);  // name
  WriteEmptyEntryList(bs);
  EncodeString(bs, emptyString);
  EncodeString(bs, emptyString);
  WriteEmptyEntryList(bs);
  // inner != 2, so the trailing 6-short block is present (FUN_100d45c0).
  for (int i = 0; i < 6; ++i) {
    bs.WriteCompressed(static_cast<uint16_t>(0));
  }
}

bool WorldServiceSerializer::Read(RakNet::BitStream& bs,
                                  Packet::WorldService* data) const {
  if (!bs.ReadCompressed(data->playerId)) return false;

  // TO BE REVISITED: the vortex terminal sends its own world-service requests
  // (open ack, destination list, node selection, purchase). Only the leading
  // discriminator is read (the per-discriminator bodies are not modelled yet), so
  // the handler can react to it without the packet surfacing as a read error.
  if (!bs.ReadCompressed(data->discriminator)) return false;

  return true;
}

}  // namespace Packet
}  // namespace FOMNetwork
