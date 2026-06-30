# Avatar

`FOM::Types::Player::Avatar` — the player's appearance and worn-equipment block.
50 bytes, packed, all fields `u16`. The client copies it verbatim from the
engine's `PLAYERDATA_AVATAR` block into each [[WorldUpdate]] (`+0x22`).

| Offset | Field | | Offset | Field |
| --- | --- | --- | --- | --- |
| `0x00` | `sex` | | `0x10` | `slotShirt` |
| `0x02` | `skinColor` | | `0x12` | `slotBottoms` |
| `0x04` | `face` | | `0x14` | `slotShoes` |
| `0x06` | `hair` | | `0x16` | `slotHat` |
| `0x08` | `factionId` | | `0x18` | `slotHead` |
| `0x0a` | `rankId` | | `0x1a` | `slotEyes` |
| `0x0c` | *(unnamed)* | | `0x1c` | `slotShoulder` |
| `0x0e` | `legacyFaction` | | `0x1e` | `slotArms` |
| | | | `0x20` | `slotTorso` |
| | | | `0x22` | `slotBack` |
| | | | `0x24` | `slotLegs` |

The first six fields (`sex`/`skinColor`/`face`/`hair` …) are the character
creation choices — the same values stored server-side in the `player` row (see
the server vault). Reproduce: `fomre type /FOM/Types/Player/Avatar`.
