# SharedMemory

`FOM::SharedMemory` is the client's central **state blackboard**: a single
named, memory-mapped block that every module reads game state from and writes it
to, keyed by a slot index. If a client routine needs "what's my player id / what
world am I in / what weapon is active", it reads it from here.

## The backing store

Created in `FOM::SharedMemory::Initialize` (`CShell.dll` rva `0x7720`):

```c
DAT_10351150 = CreateFileMappingA(-1, &sa, PAGE_READWRITE, 0, 0x7bca4, "WhatAreULookingAt?");
g_pSharedMemory = MapViewOfFile(DAT_10351150, FILE_MAP_WRITE, 0, 0, 0);
memset(g_pSharedMemory, 0, 0x1ef29);
```

- **Named section:** `"WhatAreULookingAt?"` (a page-file-backed mapping, so any
  process in the session can open it — handy for external tooling).
- **Size:** `0x7bca4` bytes = `0x1ef29` (126761) **4-byte slots**.
- **Pointer:** `FOM::SharedMemory::g_pSharedMemory` (`CShell.dll` data rva
  `0x35a6fc`).

## Addressing — slot index, not byte offset

Every typed accessor indexes by `slot * 4`. From
`FOM::SharedMemory::ReadUInt` (rva `0x79b0`):

```c
uint ReadUInt(SharedMemory index) {
  if (0x1ef28 < index) return 0;          // bounds check
  Lock();
  uint v = *(uint *)(g_pSharedMemory + index * 4);
  Unlock();
  return v;
}
```

So a field's byte offset is `index * 4`, e.g. `PLAYER_ID` (slot 91) → byte
`0x16c`. Access is guarded by `Lock()`/`Unlock()` for concurrent readers/writers.

### Accessors

Typed readers `ReadByte / ReadUShort / ReadUInt / ReadULongLong / ReadBool /
ReadFloat / ReadDouble`, plus `…Array`, `ReadString`, `FindIndex`, and the
matching `Write*` (e.g. `WriteByte`). Each comes in two overloads — one over the
global block, one over a caller-supplied buffer.

## The `FOM::Enums::SharedMemory` slot map

Slot indices (selected — full enum via `fomre type /FOM/Enums/SharedMemory`):

| Slot | Name | Meaning |
| --- | --- | --- |
| 0 | `SHAREDMEMORY_WORLD_ID` | current world id |
| 84 | `SHAREDMEMORY_IS_LOGGED_IN` | logged into master |
| 85 | `SHAREDMEMORY_IS_LOGGED_INTO_WORLD` | in a world (gates [[Player Update Flow]]) |
| 91 | `SHAREDMEMORY_PLAYER_ID` | local player id |
| 119 | `SHAREDMEMORY_APARTMENT_ID` | apartment instance (world 4) |
| 140 | `SHAREDMEMORY_IS_CONNECTED_TO_WORLD` | world connection state |
| 148 | `SHAREDMEMORY_IS_CLONING_COMPLETE` | respawn/clone done |
| 12350 | `SHAREDMEMORY_ACTIVE_WEAPON` | equipped weapon id |
| 120479 | `SHAREDMEMORY_ACTIVE_IMPLANTS` | implant bitfield |

Note the large gaps — the block also holds wide regions (names, inventory,
arrays) addressed by the higher slot numbers.

## Verified live

With a character in-world (player 1 "Tom Dev", world 1 Manhattan), reading the
block through the live pointer confirms both the addressing model and the
values:

```
g_pSharedMemory @ 0x7673a6fc -> block 0x34e0000
  slot      0 WORLD_ID               = 1     # Manhattan
  slot     84 IS_LOGGED_IN           = 1
  slot     85 IS_LOGGED_INTO_WORLD   = 1
  slot     91 PLAYER_ID              = 1     # matches account/player id 1
  slot    119 APARTMENT_ID           = 0
  slot  12350 ACTIVE_WEAPON          = 0     # unarmed
```

Reproduce: dereference `g_pSharedMemory` (`CShell.dll:0x1035a6fc`, `--type ptr`),
then read `block + slot*4`.

## Why it matters for the emulator

This block is the source of truth the client serializes from. [[Player Update
Flow]] shows `FillUpdate` reading ~20 of these slots to assemble each
[[WorldUpdate]] — so the slot map is effectively the field list the server
receives.
