# World Objects

How the server places **interactable, non-player objects** into the world —
vortex terminals, storage lockers, mining rigs, deployable shields, and the like.
These are the "deployables" the game lets players place. The client models them as
**static world objects** (LithTech objects named `SWO(...)`), spawned and updated
entirely from the server via a single discriminated packet, `ID_WORLD_OBJECTS`
(133, `0x85`).

This is the "object / placement system" the [[Vortex Gates]] note flagged as the
missing prerequisite for opening terminals by *using a placed object* (rather than
the walk-in-gate stand-in).

`ID_WORLD_OBJECTS = 133` was **known but unimplemented** — commented out in
`fom-network/include/fom-network/enums/PacketIdentifier.h:56` and
`server-shared/Core/Enums/PacketIdentifier.cs:67`. This document is the RE behind
implementing it. The sibling packets `ID_OBJECT_DETAILS` (161), `ID_STORAGE`
(153), `ID_MINING` (154), `ID_PRODUCTION` (155) are the *interaction* side (open a
placed object's terminal UI) and are still unimplemented.

## The packet — a discriminated union (server → client)

Like [[Vortex Gates|ID_VORTEX_GATE]] and [[WorldUpdate]], `ID_WORLD_OBJECTS` is
one id carrying several operations, selected by a leading **sub-type** byte. All
decode/encode is in `Object.lto` (the gameplay object module), not `CShell.dll`:

| Function | rva (Object.lto) | Role |
| --- | --- | --- |
| `Packet_ID_WORLD_OBJECTS::Read` | `0x568d0` | sub-type switch, deserialize |
| `Packet_ID_WORLD_OBJECTS::Write` | `0x56530` | symmetric serialize |
| `HandlePacket_ID_WORLD_OBJECTS` | `0x56ac0` | applies the decoded packet to the world |
| `FUN_10051240` | `0x51240` | **spawns** a category's objects as `StaticWorldObject`s |
| `FUN_100dc250` / `FUN_100db970` | `0xdc250`/`0xdb970` | object-vector read / write element |

```
[ VariableSizedPacket base | subType : compressed u8 | <arm-specific> ]
```

| subType | Meaning (RE) | Payload |
| --- | --- | --- |
| 1 | **Full snapshot** — every category's object vector at once | one object-vector per category (fixed order, see below) |
| 2 | **Category update** — replace/add one category's objects | `category : compressed u16`, then one object-vector |
| 3 | **Set state flag** on an existing object | `category : u16`, `objectId : u32`, `flag : 1 bit` |
| 4 | **Per-object detail update** | `category : u16`, `objectId : u32`, `value : u8` |

Sub-types 1 and 2 both carry **object vectors**; the only difference is 1 sends
all categories and 2 targets a single named category. Sub-types 3/4 mutate an
already-spawned object (found by `objectId` within `category`) — 3 sets a boolean
at object `+0x40`/`+0x4e`, 4 calls `FUN_100734f0`.

## Object categories

The category is a `u16` in the range `0x1fa–0x204` (506–516). Each corresponds to
a different kind of world object, and the client keeps a separate vector per
category. Most categories share the **generic 28-byte object record** below and
are spawned by `FUN_10051240`; three use larger, distinct records:

| Category | Record size | Reader | Notes |
| --- | --- | --- | --- |
| `0x1fa`,`0x1fb`,`0x1fd`,`0x1fe`,`0x1ff`,`0x200`,`0x203`,`0x204` | 28 B (`0x1c`) | `FUN_10051240` | generic `StaticWorldObject` (this doc) |
| `0x1fc` (508) | 792 B (`0x318`) | `FUN_100530d0` | large record — not yet RE'd |
| `0x201` (513) | 84 B (`0x54`) | `FUN_100535d0` | not yet RE'd |
| `0x202` (514) | 196 B (`0xc4`) | `FUN_100515c0` | not yet RE'd |

The semantic name of each generic category (which is a vortex terminal vs. a
storage locker vs. a mining rig) is **not yet pinned** — the rendering path
(`FUN_10051240`) is identical for all of them, so the category most likely drives
*interaction* (which terminal packet the "use" action triggers) rather than the
model. The emulator uses `0x1fa` as a generic deployable category for now.

## The generic object record (28 bytes)

`FUN_100db970` (write) / the loop in `FUN_100dc250` (read) encode each record.
The vector is a **compressed u32 count** followed by `count` records. Every field
is `WriteCompressed`; wire order (which is *not* memory-offset order — position
sits at `+0x08` in memory but is written last):

| # | Field | Mem off | Encoding | Meaning |
| --- | --- | --- | --- | --- |
| 1 | `id` | `+0x00` | compressed u32 | server-assigned object instance id (used in the engine name `SWO(cat)_id`) |
| 2 | `type` | `+0x04` | compressed u16 | **`ItemType`** — indexes `g_ItemDefTable`; the item's `model` is the object's model |
| 3 | `state` | `+0x06` | compressed u8 | state/variant byte (passed to `FUN_10073630`) |
| 4 | `extra` | `+0x18` | compressed u32 | owner / faction / context (passed through to the object) |
| 5 | `position` | `+0x08` | `PositionRotation` | placement — precision **16** (compressed u16 per axis + 9-bit yaw) |

`PositionRotation::Read` (the object element) uses a default-constructed
`PositionRotation`, whose precision is `0x10` — so `position` takes the `>15`
branch: each axis a compressed `u16`, then the 9-bit packed yaw, exactly like the
main player position in [[WorldUpdate Wire Format]]. So a server can place an
object at a player's captured update position verbatim.

## Rendering — `FUN_10051240` (the spawn)

For each record in a generic category's vector the client:

1. Looks up `g_ItemDefTable[type]` (guard: `1 <= type <= 3009` and the def is
   non-null). **`type` is an `ItemType`.**
2. Reads the model string from `ItemDefinition + 0x10` (`model` — see
   [[Item Definitions]]), plus icon/skin, defaulting to `""` if null.
3. Names the engine object `sprintf("SWO(%u)_%u", category, id)` — **S**tatic
   **W**orld **O**bject.
4. Creates a `"StaticWorldObject"` engine object at the record's position
   (`FUN_10073630(obj, x, y, z, rot, state, extra)`), keyed by `(category, id)`
   so later sub-type 2/3/4 updates can find it.

> **Renderability caveat.** The object only shows a mesh if its `ItemType`'s
> `ItemDefinition.model` is populated. In the live client several nominally
> "deployable" types (`DeployableShield` 984, `VortexReactor` 994,
> `PortableVortexParticleEmitter` 106) have **near-empty definitions** (no model)
> — likely cut/placeholder content. Placing them still creates the object without
> crashing, but with no visible mesh. Types with real world-object models
> (verified live by scanning `g_ItemDefTable`): **999** `Props/Terminals/safe.ltb`,
> **995** `productionterminal.ltb`, **997** `marketing_terminal.ltb`, **996**
> `repunit.ltb`, **36** `Props/turret_wall.ltb`. The packet/placement is correct
> regardless of model.

## Flow

```
Server  --ID_WORLD_OBJECTS{sub 2, category, [ {id,type,state,extra,pos} ... ]}-->  all clients
        client spawns/updates one StaticWorldObject per record, keyed (category,id)
Server  --ID_WORLD_OBJECTS{sub 3/4, category, id, ...}-->  clients   (mutate a live object)
```

The subsequent "use this object" interaction (open its storage/mining/vortex
terminal) is a *separate* packet (`ID_STORAGE`/`ID_MINING`/`ID_PRODUCTION` /
`ID_WORLDSERVICE`) and is not yet implemented — see [[Vortex Gates]] for the
`ID_WORLDSERVICE{5,0xc}` terminal-open path this will eventually hang off.

## Server-side status (emulator)

A first increment is implemented server→client and **verified live** (a storage
safe and a wall turret placed via chat, rendered on the floor at the player's
position — coordinate space, model lookup and wire format all confirmed):
- `ID_WORLD_OBJECTS` (133) is defined natively + managed as a **write-only**
  packet `{ subType, category, count, WorldObject[] }` (the client never sends it),
  with a `WorldObjectUpdate` sub-type enum. Only sub-type 2 (category update) is
  written today, under category `0x1fa`.
- A world-object registry holds placed objects; a **plain-chat** `deploy [itemType]`
  command (NOT a slash command — the client parses `/`-commands itself and never
  sends unknown ones as chat) places one at the player's current position and
  broadcasts it, and newly-joining players are sent the existing objects on entry.

**Not yet done:** the C→S placement request (`ID_DEPLOY_ITEM` 172 — the client's
send site was not located; deploy is currently GM/chat-driven, not player-item
driven); real deployable item master data / models; the interaction packets that
open a placed object's terminal; the larger category records (`0x1fc`/`0x201`/
`0x202`); persistence of placed objects; and interest/grid scoping.

## Reproduce

```bash
fomre decompile "Object.lto:0x100568d0"   # Packet_ID_WORLD_OBJECTS::Read (sub-type switch)
fomre decompile "Object.lto:0x10056530"   # Write (symmetric)
fomre decompile "Object.lto:0x10056ac0"   # HandlePacket_ID_WORLD_OBJECTS
fomre decompile "Object.lto:0x10051240"   # spawn -> StaticWorldObject, model = ItemDef+0x10
fomre decompile "Object.lto:0x100dc250"   # object-vector read (record format)
fomre decompile "Object.lto:0x100db970"   # object-vector write element
```

See [[Vortex Gates]] (the interaction this unblocks), [[Item Definitions]]
(`type` → model), [[WorldUpdate Wire Format]] (the shared position encoding), and
[[Packet Transport]] (the compressed-int codecs / envelope).
