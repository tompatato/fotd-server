#include <fom-network/packets/CheckMail.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

bool CheckMailSerializer::Read(RakNet::BitStream& bs,
                               Packet::CheckMail* data) const {
  if (!bs.ReadCompressed(data->playerId)) return false;

  return true;
}

}  // namespace Packet
}  // namespace FOMNetwork
