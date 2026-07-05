#pragma once

#include <fom-network/Interop.h>

namespace FOMNetwork {
namespace Packet {

// Client -> world request sent on world entry: "is there new mail?". The client
// blocks vortex travel with a "checking for new mail" message until it receives
// the ID_MAIL reply, so the server must always answer.
#pragma pack(push, 1)
struct CheckMail {
  uint32_t playerId;
};
#pragma pack(pop)

ASSERT_BLITTABLE(CheckMail);

}  // namespace Packet
}  // namespace FOMNetwork
