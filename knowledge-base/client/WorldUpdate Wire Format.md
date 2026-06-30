# WorldUpdate Wire Format

The exact RakNet `BitStream` encoding produced by `FOM::Types::WorldUpdate::Write`
— i.e. the body of an `ID_UPDATE` packet (see [[Player Update Flow]]). This is the
bit-packed form on the wire, *not* the 200-byte in-memory [[WorldUpdate]] struct.
All fields are written **little-endian, LSB-first** (byte-reversed first only when
`BitStream::DoEndianSwap()` is true, i.e. on big-endian hosts).

Functions (all `CShell.dll`):

| Function | rva | Role |
| --- | --- | --- |
| `WorldUpdate::Write` | `0x1a1440` | writes `type`, dispatches by variant |
| `WorldUpdate::WritePlayer` | `0x1a1310` | player header, then calls WriteCharacter |
| `WorldUpdate::WriteCharacter` | `0x1a00b0` | id, position, avatar, combat/emote state |
| `PositionRotation::Write` | `0x26be40` | position + 9-bit yaw |
| `PositionRotation::WritePosition` | `0x272420` | quantized x/y/z |
| `Player::Avatar::Write` | `0x2575d0` | appearance + equipment |

Reproduce: `python3 tools/re/fomre.py decompile CShell.dll:0x101a1440` (etc.).

## Encoding primitives

- **`WriteCompressed(v, N, unsigned)`** — RakNet's variable-length form: drops
  leading all-zero bytes, so small values cost few bits. Used for `type`, the
  grids, and ids.
- **`WriteCompressed_T<uint/ushort>`** — same, templated per width.
- **`WriteBits(v, N)`** — exactly `N` raw low bits.
- **`Write1()` / `Write0()`** — a single presence/flag bit. The pervasive pattern
  is *"1 + payload, or 0"* — optional fields cost 1 bit when absent.

## Top level — `Write`

1. `type` — `WriteCompressed(8)`. `1` = player, `2` = character, `3`/`4` other.
   The client only sends **type 1** (see [[Player Update Flow]]).
2. dispatch → `WritePlayer` (type 1).

## Player header — `WritePlayer`

| # | Field | Encoding |
| --- | --- | --- |
| 1 | `grid1` | compressed u32 |
| 2 | `grid2` | compressed u32 |
| 3 | `visibilityAreaId` | compressed, 8 bits (low byte) |
| 4 | `targetingTurretId` | 1 bit; if set → compressed u32 |
| 5 | `activeMedicalTreatment` | 1 bit; if set → 3 bits |
| 6 | `Unknown1` (`field@0xb4`) | compressed u32 — server calls this `Unknown1` |
| 7 | *(character body)* | → `WriteCharacter` |

## Character body — `WriteCharacter`

| # | Field | Encoding | Notes |
| --- | --- | --- | --- |
| 1 | `id` | compressed u32 | player id |
| 2 | `position` | `PositionRotation::Write` | see below |
| 3 | `avatar` | `Avatar::Write` | see below |
| 4 | `isDead` | 1 bit | **gates everything below** |
| 5 | `verticalLookAngle` | 8 bits, written as `angle + 0x5a` (+90) | pitch, biased to unsigned |
| 6 | `animationId` | `0` bit if `==0x10`; else `1` + 12 bits | `0x10` is the idle default |
| 7 | `movementStateId` | `0` bit if 0; else `1` + 5 bits | |
| 8 | `equippedWeapon` | `0` bit if 0; else `1` + compressed u16, then ↓ | unarmed = single 0 bit |
| 8a | ↳ `isWeaponAimed` | 1 bit | only if weapon present |
| 8b | ↳ `consumedAmmo` | `0` bit if 0; else `1` + 7 bits | only if weapon present |
| 8c | ↳ `firedPosition` | `WritePosition` (no yaw) | only if `consumedAmmo != 0` |
| 9 | `wasHit` | 1 bit; if set → `hitAnimationId` 4 bits + `hitDirection` 4 bits | |
| 10 | `emoteId` | `0` bit if 0; else `1` + 6 bits | |
| 11 | *implant block* | gated by a world/feature check `FUN_102575b0` *(unverified)* | |
| 11a | ↳ `activeImplants` | `0` bit if 0; else `1` + compressed u16 | |
| 11b | ↳ `shieldSetting` | 7 bits | only if an implant-capability check passes *(unverified)* |
| 12 | `movementSpeed` | 8 bits | |
| 13 | `field@0xb0` | 3 bits | server `field34`; semantics unverified |
| 14 | `field@0xb8` | 1 bit | flag; unverified |
| 15 | `field@0xbc` | 10 bits | unverified |
| 16 | `field@0xc0` | 10 bits | unverified |
| 17 | `isShieldActive` | 1 bit | last field |

> When `isDead` is true, fields 5–17 are **omitted entirely** — a dead-player
> update is just header + id + position + avatar + the dead bit.

## Position — `PositionRotation::Write` / `WritePosition`

[[PositionRotation]] writes `WritePosition`, then `rot` as **9 bits** (packed yaw).

`WritePosition` branches on `pos.precision` (a *bit width*, not transmitted here —
both ends must agree on it out of band, presumably from world/grid context —
**unverified how the reader obtains it**):

- `precision <= 15` (the common path): for each axis write **|value|** in
  `precision` bits (magnitude via two's-complement abs), then **3 sign bits**
  (x, y, z; `1` = negative). So a position costs `3*precision + 3` bits.
- `precision >= 16`: fall back to a full per-axis compressed write
  (`FUN_1010ee30`, *unverified* — likely `WriteCompressed` of the raw i16).

See [[PositionRotation]] for the in-memory layout. Coordinates are quantized
signed integers, not floats.

## Avatar — `Avatar::Write`

Written in this order (bit widths in parens):

`sex`(1), `skinColor`(1), `face`(5), `hair`(5), `factionId`(**32**), `rankId`(5),
`field@0xc`(6), `legacyFaction`(4), `slotShirt`(12), `slotBottoms`(12),
`slotShoes`(12).

Then an **extended-equipment flag**: if any of the extended slots (`slotHat`
through `slotHands`) is non-zero → write `1` followed by 9 × 12-bit slots
(`slotHat, slotHead, slotEyes, slotShoulder, slotArms, slotTorso, slotBack,
slotLegs, slotHands`); otherwise a single `0` bit and they're skipped.

Finally four 1-bit flags: `isCommander`, `field@0x2a`, `field@0x2c`,
`isGroupLeader`.

> `Avatar::Write` references fields beyond what [[Avatar]] currently documents
> (`slotHands`, `isCommander`, `field@0x2a/0x2c`, `isGroupLeader`) — the in-memory
> [[Avatar]] note covers the type-DB layout; this section is the serialization order.

## Server side

The managed deserializer mirrors this in
[`server-shared/Core/Packets/Types/WorldUpdate.cs`](../../server-shared/Core/Packets/Types/WorldUpdate.cs);
its `PlayerUpdate` field order (`Grid1, Grid2, VisibilityAreaId,
TargetingTurretId, ActiveMedicalTreatment, Unknown1, Character`) matches
`WritePlayer` exactly, confirming `field@0xb4` = `Unknown1`.
