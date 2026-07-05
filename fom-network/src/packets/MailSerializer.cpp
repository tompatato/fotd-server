#include <fom-network/packets/Mail.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

void MailSerializer::Write(RakNet::BitStream& bs,
                           const Packet::Mail* data) const {
  bs.WriteCompressed(data->playerId);
  // Mail list: a compressed count followed by that many entries. Only the empty
  // case is written today, so no entries follow.
  bs.WriteCompressed(data->mailCount);
  // Trailing "has appended block" flag the client reads after the list; always
  // false for the empty reply.
  bs.Write(false);
}

}  // namespace Packet
}  // namespace FOMNetwork
