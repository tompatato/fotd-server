# Game Master Commands

How a typed `/`-command turns into a packet on the wire. Every staff/GM command
(kick, summon, spawn, …) is sent as a **single** packet type,
`Packet_ID_GAMEMASTER` (identifier byte **`0xb3` = 179**); the specific command is
a numeric discriminator inside the payload. Whether the local player is *allowed*
to run a command is decided before this by [[Account Access Levels]]; this note is
about what actually goes out once a command is dispatched. All addresses are
`CShell.dll` RVAs (image base `0x10000000`).

## Dispatch → build → send

`/`-lines are routed to the dispatcher `FUN_100ff420` (see [[Account Access Levels]]),
which resolves the command name to an id (`FUN_100f8370`: kick=1 … spawn=`0xd` …)
and, if the access-level gate passes, calls the executor **`FUN_100fe3a0(commandId,
argString)`** (rva `0xfe3a0`).

`FUN_100fe3a0` is one big `switch(commandId)`. Each case parses the argument string
(`sscanf`), fills command-specific fields, and calls `FOM::SendPacket(pkt, dest,
…)` — most commands go to `MASTER`, world-affecting ones (spawn, npc, invis, god,
announce-to-world) go to `WORLD`. The packet object is constructed by
`FUN_100fc4b0` (rva `0xfc4b0`), which sets the vftable to `Packet_ID_GAMEMASTER`
and the identifier byte to `0xb3`.

## Wire format (`Packet_ID_GAMEMASTER`, id 179)

Serialization is the packet's own `Write` method, `FUN_100fac60` (rva `0xfac60`).
It always emits a common header, then a per-command tail (`switch(commandId)`).
Every field is a RakNet **compressed** integer (see [[Packet Transport]]).

| # | Field | Enc | Notes |
| --- | --- | --- | --- |
| 1 | `playerId` | compressed u32 | the issuing player |
| 2 | `commandId` | compressed u16 | e.g. `0xd` = spawn |
| 3… | *command-specific tail* | — | see below |

The tails vary widely: string commands (kick/locate/teleport, ids 1/3/4) append one
`EncodeString`; kickban (2) a string + u32; announce (0xa/0xb) a string; vortex (0xc)
a u32; etc. Only **spawn** is modelled server-side so far.

### `spawn` tail (commandId `0xd`)

Usage string: `/spawn [type] [0(normal)/1(secured)/2(bound)/3(special)] [quantity]`.
`FUN_100fe3a0` case `0xd` parses `%i %i %i` = *type, security, quantity*, looks the
type up in the item catalog (`g_ItemDefTable`, see [[Item Definitions]]) — bailing
with "Invalid item…" if unknown — and builds an [[Inventory|`Item`]] descriptor
(the `[0/1/2/3]` arg becomes the item's `security` field). The tail then is:

| # | Field | Enc | Notes |
| --- | --- | --- | --- |
| 3 | `Item.id` | compressed u32 | instance id (0 from the client — server assigns) |
| 4 | `Item.base` | `ItemBase` | full [[Inventory]] `ItemBase` (`FUN_10255040` → `ItemBase::Write` rva `0x254d40`) |
| 5 | `quantity` | compressed u32 | how many to spawn (top-level, separate from `ItemBase.value`) |

The item descriptor is written by `FUN_10255040` (rva `0x255040`): a compressed
u32 id followed by `ItemBase::Write`. This is byte-for-byte the same `Item` /
`ItemBase` encoding used by [[Inventory]] item lists, so the server reuses the
existing `ItemSerializer` to read it.

> Note the redundancy: `ItemBase.value` (field 2 of `ItemBase`) is the stack
> count, *and* there is a separate top-level `quantity`. The server treats the
> top-level `quantity` as the number of instances to grant.

> The client sends `ItemBase.value = 0` for non-stackable items. The
> `ID_ITEMS_ADDED` pickup popup shows `value × instanceCount`
> (`HandlePacket_ID_ITEMS_ADDED` → `FUN_1023ed50`: `ItemBase.value` at
> `ItemStack+0x2` times the `ids`-set size), so a value of 0 renders as "+0". The
> server floors `value` at 1 when granting so the count reflects reality.

## Server side

Sent to the **world** server, which reads it as `ID_GAMEMASTER` and — for the spawn
command — grants the item(s) to the issuing player and replies with
[[Inventory|`ID_ITEMS_ADDED`]] (147) with **`dest = 1`** (the "merge into the
backpack + refresh" path; the client has no `dest == 0` case, so sending 0 makes it
silently drop the items). Access is *not* re-checked server-side yet; the client's
[[Account Access Levels]] gate is currently the only guard.

## Reproduce

```bash
fomre decompile "CShell.dll:0x100fe3a0"   # GM command executor (switch by command id)
fomre decompile "CShell.dll:0x100fc4b0"   # packet ctor: vftable=Packet_ID_GAMEMASTER, id=0xb3
fomre decompile "CShell.dll:0x100fac60"   # Packet_ID_GAMEMASTER::Write (header + per-command tail)
fomre decompile "CShell.dll:0x10255040"   # spawn item descriptor: u32 id + ItemBase::Write
fomre decompile "CShell.dll:0x10254d40"   # ItemBase::Write (field wire order)
```

See [[Account Access Levels]] (who may run a command), [[Inventory]] (the `Item` /
`ItemBase` encoding), and [[Packet Transport]] (the compressed-int codecs).
