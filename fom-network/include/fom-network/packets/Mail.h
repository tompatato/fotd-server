#pragma once

#include <fom-network/Interop.h>

namespace FOMNetwork {
namespace Packet {

// World -> client mail delivery / check result. Only the empty-inbox reply is
// implemented today: the client reads a mail count and (when non-zero) that many
// mail entries, so a count of 0 is a complete "no new mail" answer that clears
// the client's vortex gate. The full inbox payload is left for a later mail
// feature.
#pragma pack(push, 1)
struct Mail {
  uint32_t playerId;
  uint8_t mailCount;
};
#pragma pack(pop)

ASSERT_BLITTABLE(Mail);

}  // namespace Packet
}  // namespace FOMNetwork
