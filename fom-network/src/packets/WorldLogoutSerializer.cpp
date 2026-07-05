#include <fom-network/packets/WorldLogout.h>

#include "PacketSerializers.h"

namespace FOMNetwork {
namespace Packet {

bool WorldLogoutSerializer::Read(RakNet::BitStream& bs,
                                 Packet::WorldLogout* data) const {
  if (!bs.ReadCompressed(data->playerId)) return false;

  // The client writes isChangingWorlds as a single bit following the
  // (compressed) player id, so it is read back as a bool bit here.
  bool isChangingWorlds = false;
  if (!bs.Read(isChangingWorlds)) return false;
  data->isChangingWorlds = isChangingWorlds ? 1 : 0;

  return true;
}

void WorldLogoutSerializer::Write(RakNet::BitStream& bs,
                                  const Packet::WorldLogout* data) const {
  bs.WriteCompressed(data->playerId);
  bs.Write(data->isChangingWorlds == 1);
}

}  // namespace Packet
}  // namespace FOMNetwork
