# Vortex Gates

How a player travels between worlds in-game. The **vortex** is the fiction's
faster-than-light transit network; in the client it is a terminal
(`CWindowTerminalVortex`) where you pick a **destination world** and a **node**
(spawn point) within it, then confirm. The key surprise: the vortex packet does
**not** connect you to anything itself. It is a short request/approve exchange
over a single discriminated packet (`ID_VORTEX_GATE`, 123) whose only lasting
effect is to stage the destination in [[SharedMemory]]; the actual server switch
is then driven entirely by the existing [[World Login Handoff]].

So vortex travel is two stages bolted together:

```
Vortex terminal ──ID_VORTEX_GATE──▶ approve ──stages ENTERING_WORLD_ID/NODE_ID──▶ [World Login Handoff]
```

`ID_VORTEX_GATE = 123` (`0x7b`) is **known but unimplemented** in the emulator —
it is commented out in both `fom-network/include/fom-network/enums/PacketIdentifier.h:46`
and `server-shared/Core/Enums/PacketIdentifier.cs:53`. This document is the RE
needed to implement it.

## The packet — a discriminated union

Unlike most packets, `ID_VORTEX_GATE` is **one packet ID carrying several
operations**, selected by a sub-type byte — the same shape as the
[[WorldUpdate]] update union. Every arm shares a common prefix and then varies:

```
[ VariableSizedPacket base | playerId : compressed-uint | subType : u8 | <arm-specific> ]
```

Built by `FUN_101064c0` (`CShell.dll` rva `0x1064c0`) — it sets the vtable to
`Packet_ID_VORTEX_GATE::vftable` and stamps the id byte `0x7b`. Serialized by
`FUN_10105d80` (rva `0x105d80`), which writes the base, the compressed `playerId`,
the `subType` byte, then switches on `subType`:

| subType | Direction | Extra payload | Meaning (RE) |
| --- | --- | --- | --- |
| 1 | **Client→World** | *(none)* | **gate activated** — a physical gate's countdown elapsed (see below) |
| 2 | — | `ItemList` | vortex ticket / cost items (see [[Inventory]] `ItemList`) |
| 3 | — | `uint`, `u8`, `u8` | *unconfirmed* (a message-box confirm variant) |
| 4 | **Server→Client** | `world : u8`, `node : u8` | **travel approved** — go to `world`/`node` |
| 5 | **Client→Master** | *(none)* | open vortex / request destination list |
| 6 | Server→Client | *(list blob)* | terminal data — populate the vortex window |
| 7 | **Client→World** | `world : u8`, `node : u8` | **travel request** — take me to `world`/`node` |

Bytes are written with `FUN_10038910` = `BitStream::WriteCompressed(&b, 8, true)`.
The two travel arms (4 and 7) share the identical `{world, node}` tail. Arms 2/3
are reached from `CWindowMessageBox::OnEvent` confirmation dialogs (4 of the 8
`FUN_101064c0` call sites) and are not on the world-travel path; they are recorded
here for completeness and left partly unconfirmed.

> **Sub-type 1 (gate activation)** was missed in the first pass because it is
> absent from the `FUN_10105d80` serializer switch — it carries no payload beyond
> `playerId`, so it falls through the default case. It is sent by
> `CWindowVortexImp::Render` (rva `0x17d720`): a physical vortex gate shows a
> countdown (`"{N} sec"`), and when that elapses the client sends
> `{sub 1, playerId}` to the **world**. This is the trigger for a *walk-in* gate,
> as opposed to the terminal's sub-type 7 destination selection. Confirmed live:
> the client sends only sub-type 1 while standing in a gate.

## Two distinct vortex interactions

RE of the client's window model (deeper dive) shows the vortex is **two separate
mechanisms**, not one:

1. **Walk-in gate** — `CWindowVortexImp`. Stand in a gate, a countdown runs
   (`"{N} sec"`), and on completion the client sends `ID_VORTEX_GATE {sub 1}` to
   the world. **No destination** is chosen — it is a fixed-destination portal.
   This is what a player standing in a gate uses today.
2. **Vortex terminal** — `CWindowTerminalVortex`, titled *"World Service Control"*
   (string `985`), with a companion `CWINDOW_NODE_SELECTION` (`986`). This is the
   destination **picker** (world list + node list) and the only path that lets a
   player choose where to go. Its events emit:
   - event 4 (shown): **`ID_WORLDSERVICE` (165)** `{disc 0x12, playerId}` → world
     — note this is a *second* unimplemented packet the terminal depends on;
   - event 5: `ID_VORTEX_GATE {sub 5}` → **master** (destination-list request);
   - event 9: `ID_VORTEX_GATE {sub 7, world, node}` → world (travel confirm).

**Window lifecycle:** every window (both of the above) is pre-created once in
`CWindowMgr::InitAllWindows` → `CreateWindow` (vortex terminal ctor `FUN_10176fc0`,
window id `CWINDOW_TERMINAL_VORTEX`). So `GetWindow(CWINDOW_TERMINAL_VORTEX)`
always returns the object; the terminal is *shown* separately via
`CWindowMgr::ShowWindow` (sets the active-window pointer at `+0x1ac` and calls the
window's `SetupControls`/activate/show vtable slots).

**What shows the terminal — solved.** The server opens terminals by sending
**`ID_WORLDSERVICE` (165)**; the client handler `FUN_10197580` switches on
`{outer, inner}` and shows the matching window. `{5, 0xc}` opens
`CWINDOW_TERMINAL_VORTEX`. Confirmed live: sending `ID_WORLDSERVICE{5,0xc}` makes
the "Vortex Network" menu render. The `{outer=5}` open-body format is in
`FUN_100d4620` (id, inner disc, 2 bytes, 2 uints, 3 strings, two 10-entry lists
whose entries are gated by a presence bit, and 6 trailing shorts).

> **TO BE REVISITED — the real trigger is a placed terminal object.** In the game
> this menu is opened by *using a vortex-terminal object* in the world (the same
> way market/apartment terminals open). That needs the object/placement system,
> which the emulator doesn't have yet (a major feature). As an interim stand-in,
> the server opens the menu in response to the **walk-in gate's sub-type 1**. When
> placements land, move the `ID_WORLDSERVICE{5,0xc}` open to the terminal-object
> "use" path and stop hijacking the gate.

**Populating the menu — working.** When the terminal is shown it sends
`ID_WORLDSERVICE` discriminator `0x12` to the world; the server answers with
`ID_VORTEX_GATE` **sub-type 6** (destination list) and the client fills the menu
(`FUN_10199270`→`FUN_10177a40`). Sub-type 6 wire format (`FUN_1026f2e0`):
`count:u8`, then per entry `{ worldId:u8, ip:32 bits stored inverted, port:16 bits,
extra:u16 }`, then three trailing u32s (player/grid context). Confirmed live: the
menu populates with reachable worlds, node lists, and descriptions.

> **TO BE REVISITED — hardcoded list.** The sub-type 6 body is currently a fixed
> two-world list (Manhattan + Apartments at the local world endpoint) baked into
> `VortexGateSerializer`. Native code should not know world topology — the real
> list must be built from live data (the worlds/addresses the master knows are
> online) and carried in the packet, not hardcoded.

**Remaining for functional travel:** wire the menu's node selection + **Purchase**
flow (the terminal likely uses further `ID_WORLDSERVICE` discriminators, and/or a
purchase before it emits `ID_VORTEX_GATE` sub-type 7, which the server already
approves). Node-accurate spawn is still pending too.

## Flow (world travel)

```
Player opens vortex terminal, selects world + node, clicks Travel
Client  --ID_VORTEX_GATE{sub 7, playerId, world, node}-->  World server
                                    World validates (connected? inventory? not locked out?)
Client  <--ID_VORTEX_GATE{sub 4, playerId, world, node}--  World server (approve)
        client stages ENTERING_WORLD_ID/NODE_ID, ENTERING_WORLD_STATE=1,
        enters transfer state (FUN_101c0a60(0xb))
        │
        └── from here it is the ordinary [[World Login Handoff]]:
Client  --ID_WORLD_LOGIN{worldId, nodeId, playerId}-->  Master   (reads ENTERING_* from shared memory)
Client  <--ID_WORLD_LOGIN_RETURN{status, serverIp, serverPort}--  Master
        (SUCCESS) connect to serverIp:serverPort  -- the destination world server
```

## Client send — `CWindowTerminalVortex::OnEvent` (rva `0x177290`)

The terminal is an event-driven window; the relevant commands:

- **event 6 — pick world**: stores the selection in `this[0x1a88]`, and refreshes
  the world name / class / description panels from the string table (below).
- **event 8 — pick node**: stores the sub-selection in `this[0x1a89]`, formatting
  `"{0} - {1}"` and appending `" (Full account only)"` for nodes gated to full
  accounts (see [[Account Access Levels]]).
- **event 9 — confirm travel**: the guarded send. It requires all of:
  - network connected, `0 < this[0x1a88] <= 0x1f` (a real world is selected),
  - a live inventory (`GetPlayerData(PLAYERDATA_INVENTORY)`) — else string `1844`
    *"Error: Your inventory is full!"*,
  - a live avatar (`GetPlayerData(PLAYERDATA_AVATAR)`),
  - a **connectivity check** (`FUN_10256800`) — else string `5642` *"You cannot buy
    tickets to worlds that aren't connected to this one!"*

  On success it builds `ID_VORTEX_GATE` sub-type **7** with
  `{ playerId = SharedMemory[PLAYER_ID], world = this[0x1a88], node = this[0x1a89] }`
  and `SendPacket(pkt, WORLD, 1, 3, 0)` — to the **world server the player is
  currently in**, not the master (see [[Packet Transport]] for the destination
  argument).

- **event 5** sends `ID_VORTEX_GATE` sub-type **5** (playerId only) to the
  **master** — the destination-list request; the master holds cross-world
  topology so it is the authority on which worlds exist / are connected.

## Destination model (world + node)

Selecting a world (`this[0x1a88]`, 0–31) indexes three parallel string-table
runs, so the terminal shows a name, a class, and a description:

- **World names** — base `3100`: `3100 "- Please select world! -"`, `3101 NYC -
  Manhattan`, `3102 NYC - Brooklyn`, `3103 Tokyo - Upper`, `3104 Apartment`,
  `3105 Earth`, … `3118 NYC - Ground Zero`, … `3131 Training Center`,
  `3133 All Worlds`. (~31 entries — matches the `<= 0x1f` cap in event 9.)
- **World class** — base `3140`, indexed via `DAT_102e2954[sel*0x3c]`:
  `3140 SAFEZONE`, `3141 CLASS I WORLD` … `3145 CLASS V WORLD`.
- **World description** — base `3200`: e.g. `3201` (Manhattan), `3203` (Tokyo),
  `3218` (Ground Zero).

The node (`this[0x1a89]`, 0–25) selects a spawn point within the world:
`3249 "Default Node"` (index 0), `3247 "Random Node"` (index 0x19 = 25), and
per-world named nodes in between (`worldSel*0x14 + 0xcb1 + node`).
`3248 "Destination: %1!s! - %2!s!"` is the confirmation summary. Panel labels:
`985 "World Service Control"`, `986 "Node Selection"`.

> The `node` chosen here is the same `nodeId` that ends up at offset `0x431` of
> [[World Login Handoff|`ID_WORLD_LOGIN`]] — the vortex is where it originates.

## Server-side gating (implied by the client)

The client only *reports* failure strings; the checks are the server's to enforce.
The RE surfaces at least:

- **Connectivity** — you may only vortex to a world **connected to the current
  one** (string `5642`). The vortex network is a graph, not all-to-all.
- **Aggression lockout** — string `1806`: *"…your vortex access has been limited
  due to your recent aggressive acts. Please wait another N seconds…"*. This is
  the `VortexEmitterCountdown` attribute (`AttributeType` 51) counting down; while
  non-zero the vortex refuses travel. Related in-world items:
  `PortableVortexParticleEmitter` (106), `VortexTicket` (985), `VortexReactor`
  (994), and the GM `Vortex` command (`GamemasterCommand` `0xc`).
- **Inventory / avatar present** — the player must be fully spawned.

## Client receive — `FUN_10199270`, dispatched from `FOM::HandlePacket`

The reply reads `{ playerId, subType, world, node }` and, only if
`playerId == SharedMemory[PLAYER_ID]`:

- **subType 4 (approve)** →
  ```
  SharedMemory[ENTERING_WORLD_ID]    = world
  SharedMemory[ENTERING_NODE_ID]     = node
  SharedMemory[ENTERING_WORLD_STATE] = 1
  FUN_101c0a60(0xb)                    // client enters the world-transfer state
  ```
- **subType 6 (list data)** → fetches the `CWINDOW_TERMINAL_VORTEX` window and
  calls `FUN_10177a40` to repopulate the destination/node lists.

## The bridge to World Login Handoff

The staged shared-memory values are exactly what the `ID_WORLD_LOGIN` builder
(`FUN_101c0e10`, see [[World Login Handoff]]) consumes:

```c
if (ReadUInt(ENTERING_WORLD_STATE) == <ready>) {
    login.playerId = ReadUInt(PLAYER_ID);
    login.worldId  = (u8) ReadUInt(ENTERING_WORLD_ID);   // <- vortex world
    login.nodeId   = (u8) ReadUInt(ENTERING_NODE_ID);    // <- vortex node
    ...send ID_WORLD_LOGIN to MASTER...
    WriteUInt(ENTERING_WORLD_STATE, 3);                  // advance the handshake
}
```

This closes the loop and answers the open question in [[World Login Handoff]]
about where `ID_WORLD_LOGIN`'s `worldId`/`nodeId` come from: **the vortex node
selection**, relayed through `ENTERING_WORLD_ID` / `ENTERING_NODE_ID` in
[[SharedMemory]]. `ENTERING_WORLD_STATE` is the little state machine tying the two
packets together (`1` = approved by vortex, `3` = world-login sent).

## Server-side status

A working increment is implemented and **verified against the live client**:
`ID_VORTEX_GATE` (123) is defined natively and managed as a flat
`{ playerId, type, world, node }` packet (the `world`/`node` tail is read only for
the travel arms 4/7; other sub-types read `playerId`/`type` and are accepted so the
handler — not the deserializer — decides what to act on). The world server's
`VortexGateHandler` answers both client→world travel triggers with a sub-type 4
**approve**:

- **sub-type 1 (gate activation)** → approve travel to the primary hosted world at
  the default spawn node (a walk-in gate carries no chosen destination);
- **sub-type 7 (terminal request)** → approve the requested world if this process
  hosts it, otherwise redirect to the primary world.

The reconnect/respawn is then the existing [[World Login Handoff]]; the same-world
round-trip (leave → migrate back → re-register) works. A depends-on prerequisite,
the [[Mail Check]] handshake, had to land first — the client blocks the vortex
until its on-entry mail poll is answered.

Not yet done: the destination-list exchange (sub-types 5→6), so the terminal
shows the static string-table world list rather than a live/connected one;
node-accurate spawning (spawn node is still hard-coded in `RegisterClientHandler`
because the chosen node is not yet threaded through the handoff); and the real
gating (connectivity / aggression lockout / inventory), which the handler skips.

### What a fuller implementation still needs

A world server handler for `ID_VORTEX_GATE` would additionally:

1. Enforce the real gating on **sub-type 7** that the current handler skips:
   that the target `world` is **connected** to the current one, the aggression
   lockout (`VortexEmitterCountdown`) is zero, and inventory rules — replying
   with an error path (the client shows the strings above) rather than always
   approving.
2. Answer **sub-type 5** (to master) with **sub-type 6** carrying the live
   destination list the terminal renders, instead of relying on the static
   string-table list.
3. Thread the chosen `node` through the handoff so the player spawns at it
   (today it is echoed in the approve but ignored at spawn).

The subsequent `ID_WORLD_LOGIN` → `ID_WORLD_LOGIN_RETURN` is already covered by
[[World Login Handoff]]; the remaining vortex work is the list exchange plus the
connectivity/lockout rules and node placement.

## Reproduce

```bash
fomre sym Vortex
fomre decompile "CShell.dll:0x10177290"   # CWindowTerminalVortex::OnEvent (send)
fomre decompile "CShell.dll:0x101064c0"   # Packet_ID_VORTEX_GATE ctor (id 0x7b)
fomre decompile "CShell.dll:0x10105d80"   # serializer — the sub-type switch
fomre decompile "CShell.dll:0x10199270"   # reply handler -> ENTERING_* shared memory
fomre decompile "CShell.dll:0x101c0e10"   # ID_WORLD_LOGIN builder reads ENTERING_*
fomre xref     "CShell.dll:0x101064c0"    # all 8 packet construction sites
```

See [[World Login Handoff]] (the second half of vortex travel), [[World Logout]]
(the leave-world counterpart), [[SharedMemory]] (the `ENTERING_*` bridge),
[[Packet Transport]] (envelope + `MASTER`/`WORLD` routing), and
[[Client Architecture]].
