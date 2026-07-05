# Player Update Flow

How the client reports the local player's state to the world server. Two
functions in `FOM::Player` (both in `CShell.dll`):

- `SendUpdate` — rva `0x1a27a0` — decides *whether* to send and ships the packet
- `FillUpdate` — rva `0x1a2390` — fills a [[WorldUpdate]] from [[SharedMemory]]
  and the engine

`xref` confirms `SendUpdate` is the sole caller of `FillUpdate`.

## `SendUpdate` — gating, throttling, sending

```
if (!SharedMemory::ReadBool(SHAREDMEMORY_IS_LOGGED_INTO_WORLD)) return;
WorldUpdate u;  u.type = 1;            // player update
FillUpdate(this, &u);
build VariableSizedPacket{ messageType = ID_UPDATE, timestampType = ID_TIMESTAMP }
...decide whether to serialize u...
if (anything written) SendPacket(packet, WORLD, 1, 1, 2);
```

- **Gate:** nothing is sent unless `SHAREDMEMORY_IS_LOGGED_INTO_WORLD` is set.
- **Throttle:** an update is serialized only when at least one holds:
  - it is forced (caller flag), or
  - `now - lastSendTime > 0x299` (~665 ms since the last send), tracked in
    `this+0x324`, or
  - a significant event is pending — `consumedAmmo != 0` or `emoteId != 0`, or
    cloning-complete / active-medical-treatment conditions.

  So the baseline cadence is ~1.5 Hz, bursting on events.
- **Batching:** writes are counted (`local_104`), capped below 10 — up to 10
  `WorldUpdate::Write` records may accumulate in one packet before
  `SendPacket(..., WORLD, ...)` flushes to the **world server**.
- **Transport:** `ID_UPDATE` (= 126) as a RakNet `VariableSizedPacket` with an
  `ID_TIMESTAMP` prefix — see [[Packet Transport]] for the envelope and
  [[WorldUpdate Wire Format]] for the serialized body.

## `FillUpdate` — where each field comes from

`FillUpdate` is the authoritative map from client state → [[WorldUpdate]]. Key
sources:

| WorldUpdate field | Source |
| --- | --- |
| `id` | `SharedMemory::ReadUInt(SHAREDMEMORY_PLAYER_ID)` |
| `position.pos` | engine: `g_pLTClient` get-position vtable call, quantized |
| `position.rot` | engine rotation → packed u16 yaw |
| `grid1/grid2/visibilityAreaId` | branch on `SHAREDMEMORY_WORLD_ID`: world **4** → `APARTMENT_ID`; world **30** → avatar grid; world **31** → 0; else computed grid hash + visibility area |
| `verticalLookAngle` | engine pitch |
| `movementStateId` | slot `0x8f` |
| `emoteId` | slot `0x1d6a4`; special: slot `0x8d` → `0x3f` (and auto-clears via `WriteByte`), slot `0x1eec3` → `0x3d` |
| `isDead` | `SHAREDMEMORY_LOADING_WORLD == 2` |
| `isWeaponAimed` | aim slots `0x3041` / `0x3042` |
| `equippedWeapon` | `SHAREDMEMORY_ACTIVE_WEAPON` |
| `consumedAmmo` + `firedPosition` | only while a recent-fire counter (`this+0x328`) is non-zero |
| `avatar` | engine `GetPlayerData(PLAYERDATA_AVATAR)`, copied as 25×u16 into [[Avatar]] |
| `activeImplants` | `SHAREDMEMORY_ACTIVE_IMPLANTS` |
| `shieldSetting` | config key `"ShieldSetting"` (default 50) |
| `activeMedicalTreatment` | `SHAREDMEMORY_ACTIVE_MEDICAL_TREATMENT` |

The takeaway: the client is essentially a **serializer over [[SharedMemory]]**
plus a few engine queries (position/rotation/avatar). To emulate or interpret an
`ID_UPDATE`, this table *is* the field provenance.

## Server side

The world server receives `ID_UPDATE` in
[`world-server/Application/Handlers/UpdateHandler.cs`](../../world-server/Application/Handlers/UpdateHandler.cs):
it looks up the player by sender address, requires
`WorldUpdate.Kind == Player`, calls `player.ApplyUpdate(...)`, and queues the
player for broadcast to others. See *Server Topology* in the server vault.
