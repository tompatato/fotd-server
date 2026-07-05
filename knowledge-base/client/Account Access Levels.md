# Account Access Levels

How the client decides whether a player may run staff/GM commands. The gate is a
single **numeric access level** — and that number is the `accountType` byte the
master server sends in `ID_LOGIN_RETURN` (see [[Login Handshake]]). It is **not**
the `isVolunteer` flag, and it is **not** the bounded `AccountType` enum
(`INVALID=0, FREE=1, PREPAID=2, SUBSCRIPTION=3`) — the client treats the byte as a
raw level and compares it against thresholds up to **22**. All addresses are
`CShell.dll` RVAs (image base `0x10000000`).

## Where a `/`-command is recognised

Typed chat is submitted from `CWindowInputLine::OnEvent` (rva `0xfb330`,
event `param_2 == 1`) → `FUN_100fa4b0` (rva `0xfa4b0`, routes by input mode) →
**`FUN_100fa140`** (rva `0xfa140`). The leading-slash test is at ~`0xfa16f`:

```c
if (*text == '/') { FUN_100ff420(text); }   // command dispatcher
else { /* build Packet_ID_CHAT + SendPacket */ }  // plain chat
```

So a `/`-prefixed line never goes out as chat — it is routed locally to the
dispatcher `FUN_100ff420` (rva `0xff420`). Built-ins `/p /me /m /f` run
unconditionally; named GM commands go through the access-level gate below.

> Note: the observed "`/` won't appear when typed" symptom was **not** this gate
> and not command routing — it was a separate Wine keyboard-translation bug (OEM
> keys don't map to characters). See [[Keyboard Text Entry]]. This access-level
> gate only acts *after* a command is submitted.

## The access-level gate — `FUN_10032c40(commandId, level)` (rva `0x32c40`)

A pure predicate `bool(commandId, level)`, called from **53 UI sites**. The outer
guard is `if (10 < level)` (nothing below 11 runs any staff command); then each
command has a minimum:

| Threshold in code | Effective min level | Example commands |
| --- | --- | --- |
| `9 < level` | 11 | kick, locate, teleport, vortex, info |
| `0xb < level` | 12 | kickban, summon, invis, god, npc |
| `0xc < level` | 13 | worldannounce, globalannounce, enemy, dropinventory |
| `0xd < level` | 14 | spawn, delete |
| **`0x15 < level`** | **22** | **shutdown** (command id 8), case `0x2f` |

Command names map to ids in `FUN_100f8370` (rva `0xf8370`): kick=1, kickban=2,
locate=3, teleport=4, summon=5, invis=6, god=7, **shutdown=8**, arrest=9,
worldannounce=0xa, globalannounce=0xb, vortex=0xc, spawn=0xd, … delete=0x15,
dropinventory=0x16.

Dispatcher call site (`FUN_100ff420` @ ~`0xff7b2`):

```c
id    = FUN_100f8370(word);   // command name -> id
level = FUN_10032d40();       // read this account's access level (store index 5)
if (FUN_10032c40(id, level))  // gate
    FUN_100fe3a0(id, args);   // execute -> send GM command packet ([[Game Master Commands]])
```

> **`0x15` (21) is the constant compared with strict `<`, so 22 is the first
> value that unlocks the top tier** — matching the "developer = 22" claim. There
> is no enum constant literally named "developer"; 22 is just the highest level.

### Second, data-driven command table (separate mechanism)

`FUN_100ff420` also iterates a **23-entry** table at `window+0x18e8` (stride
`0x24`), gated by `SharedMemory::ReadBool(0x3044)`; each hit calls `FUN_101a0a40`
(rva `0x1a0a40`), which does its own permission test `(perm & 0x7a0)` against
**store index 2** (a permission bitfield, distinct from the access level). The
`0x17` (23) here is the entry count, unrelated to the `22` threshold.

## Where the level comes from (wire → storage → gate)

- `HandlePacket_ID_LOGIN_RETURN` (rva `0x196900`) reads the packet and, on
  SUCCESS / CREATE_CHARACTER, calls `FUN_1018c480(accountType)` and
  `FUN_1018c4a0(isVolunteer)`.
- `FUN_1018c480` (rva `0x18c480`) → `FUN_101c3bd0(5, accountType)` → **store index 5**.
- `FUN_1018c4a0` (rva `0x18c4a0`) → `FUN_101c3bd0(6, isVolunteer)` → store index 6.
- The gate's reader `FUN_10032d40` (rva `0x32d40`) → `FUN_101c32f0(5, …)` → reads
  **index 5** — the same slot `accountType` was written to.

Storage is not a plain global: it's an anti-tamper indirection over
`FOM::SharedMemory` (see [[SharedMemory]]). Each field is a descriptor
`{base, stride, size}` written/read with a random 0–999 shuffle index plus a
duplicate copy — write `FUN_101c3bd0`/`FUN_101c3a20` (rva `0x1c3bd0`/`0x1c3a20`),
read `FUN_101c32f0` (rva `0x1c32f0`). `accountType` = descriptor **index 5**,
`isVolunteer` = index 6. The reset routine `FUN_101a9b10` (rva `0x1a9b10`)
initialises index 5 = 1 (`FREE`, minimum) and index 6 = 0.

## Bottom line (server-side)

To unlock GM commands, the master server must put the desired level as a **raw
byte** in the `accountType` field of `ID_LOGIN_RETURN` (`LoginHandler` in the
server vault):

- `accountType = 22` → unlocks everything (incl. the `0x15 < level` top tier).
- `>= 12` → most GM commands (kick/summon/god/…); `>= 11` → lowest staff tier.
- `isVolunteer` is **not** consulted by this gate.

Our managed `AccountType` enum only defines `1/2/3`; sending 22 means writing the
raw value (the client treats it numerically, not as a bounded enum).

> **Unverified:** `/give` is **not** in the `FUN_100f8370` name table (which ends
> at `dropinventory=0x16`), and no literal "give" command string was found. If
> `/give` exists it likely comes through the data-driven `window+0x18e8` table
> (gated by `ReadBool(0x3044)` + the `& 0x7a0` bitfield on store index 2), so its
> exact tier is not confirmed from code. The general GM-command mechanism above
> *is* confirmed.

## Reproduce

```bash
fomre decompile "CShell.dll:0x10032c40"   # access-level gate (thresholds)
fomre decompile "CShell.dll:0x100f8370"   # command name -> id table
fomre decompile "CShell.dll:0x100ff420"   # /-command dispatcher
fomre decompile "CShell.dll:0x100fa140"   # leading-slash test (chat vs command)
fomre decompile "CShell.dll:0x10196900"   # HandlePacket_ID_LOGIN_RETURN
fomre decompile "CShell.dll:0x1018c480"   # accountType -> store index 5
fomre decompile "CShell.dll:0x101c32f0"   # obfuscated SharedMemory read
```

See [[Login Handshake]] (where `accountType` arrives), [[SharedMemory]] (the
store indirection), [[Packet Transport]] (the GM command packets sent on
execute), and [[Game Master Commands]] (the `ID_GAMEMASTER` wire format each
command produces — including `/spawn`, the actual item-granting command).
