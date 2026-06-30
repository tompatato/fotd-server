# WorldUpdate

`FOM::Types::WorldUpdate` is the in-memory record the client builds each tick to
describe the local player, then serializes and sends to the world server. It is
assembled by `FillUpdate` and sent by `SendUpdate` — see [[Player Update Flow]].

- **Type:** `/FOM/Types/WorldUpdate`, 200 bytes, packed.
- The leading `type` byte selects the variant: `1` = player update
  (`WORLD_UPDATE_TYPE_PLAYER`); the client only ever sends type 1.

## Layout (meaningful fields)

| Offset | Field | Type | Notes |
| --- | --- | --- | --- |
| `0x00` | `type` | u8 | 1 = player |
| `0x04` | `id` | u32 | player id (from `SHAREDMEMORY_PLAYER_ID`) |
| `0x08` | `position` | [[PositionRotation]] | quantized pos + packed yaw |
| `0x18` | `grid1` | u32 | grid / apartment / visibility key (world-dependent) |
| `0x1c` | `grid2` | u32 | secondary grid cell |
| `0x20` | `visibilityAreaId` | u16 | interest-management area |
| `0x22` | `avatar` | [[Avatar]] | 50-byte appearance + equipment block |
| `0x64` | `verticalLookAngle` | i32 | pitch |
| `0x68` | `animationId` | u32 | |
| `0x6c` | `movementStateId` | u32 | from slot `0x8f` |
| `0x74` | `isWeaponAimed` | bool | from aim slots `0x3041/0x3042` |
| `0x78` | `consumedAmmo` | u32 | non-zero only just after firing |
| `0x7c` | `wasHit` | bool | |
| `0x80` | `emoteId` | u32 | gestures (`0x3d`–`0x3f`) and emotes |
| `0x84` | `isDead` | bool | `SHAREDMEMORY_LOADING_WORLD == 2` |
| `0x86` | `equippedWeapon` | u16 | `SHAREDMEMORY_ACTIVE_WEAPON` |
| `0x88` | `firedPosition` | Position | muzzle position when firing |
| `0x94` | `hitAnimationId` | u8 | |
| `0x96` | `activeImplants` | u16 | `SHAREDMEMORY_ACTIVE_IMPLANTS` |
| `0x98` | `activeMedicalTreatment` | u8 | |
| `0x9c` | `targetingTurretId` | u32 | |
| `0xa2` | `movementSpeed` | u8 | |
| `0xa3` | `shieldSetting` | u8 | config `"ShieldSetting"`, default 50 |
| `0xa4` | `isShieldActive` | bool | |

(Several trailing `undefined` fields remain unnamed — RE-in-progress.)

## Component types

- [[PositionRotation]] (16B) — quantized `Position` (16-bit `x/y/z` + precision
  word) plus a packed `u16` yaw.
- [[Avatar]] (50B) — appearance + equipment slots, copied verbatim from the
  engine's `PLAYERDATA_AVATAR` block.

## In-memory vs. on the wire

This 200-byte struct is the **in-memory** form. What goes on the wire is the
bit-packed output of `FOM::Types::WorldUpdate::Write(update, bitStream)`,
documented field-by-field in [[WorldUpdate Wire Format]] — not a raw `memcpy` of
these 200 bytes. The server's managed mirror is
[`server-shared/Core/Packets/Types/WorldUpdate.cs`](../../server-shared/Core/Packets/Types/WorldUpdate.cs),
whose `Type { Invalid=0, Player=1, Character=2 }` matches the `type` byte here.
