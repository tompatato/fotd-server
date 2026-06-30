# Item Definitions

Every item in the game is described by an `ItemDefinition` record. The client
keeps all of them in one global, fixed-size table indexed directly by **item
type id**, so resolving an item is an O(1) array lookup — there is no hash map or
search accessor.

## The table: `g_ItemDefTable`

- Type: `ItemDefinition *[3009]` (an array of pointers, one per item type id).
- Location: `CShell.dll` data rva `0x3c3fa8` (also mirrored in `Object.lto` rva
  `0x1b9710`).
- **Indexed by item type id**: `g_ItemDefTable[type]` points to that type's
  `ItemDefinition`, or is `NULL` for unused/undefined type ids.
- Slot `0` is unused (null); valid type ids run `1 .. 3008`. ~1072 of the 3009
  slots are populated.

The individual records are the committed `ItemDef_<N>` globals — e.g.
`ItemDef_106` *is* the `ItemDefinition` for type 106, and
`g_ItemDefTable[106] == &ItemDef_106` (verified live, below).

### Access idiom

There is no `GetItemDef()` function; consumers index the table inline. From a
representative consumer (`CShell.dll` `FUN_1016a160`, one of ~460 referencers):

```c
if (((ushort)((short)type - 1U) < 0xbc0) &&            // type in 1..3008
    (def = g_ItemDefTable[type & 0xffff], def != NULL)) // skip null slots
{
    ... use def ...
}
```

So the canonical pattern is: bounds-check `type-1 < 0xbc0` (3008), read
`g_ItemDefTable[type]`, and null-check before use.

## The `ItemDefinition` struct (144 bytes)

Reproduce: `fomre type /FOM/Types/Item/ItemDefinition`. Identified fields:

| Offset | Field | Type | Notes |
| --- | --- | --- | --- |
| `0x00` | `type` | u16 (`ItemType`) | item type id (matches its table index) |
| `0x08` | `category` | u8 (`ItemCategory`) | weapon / ammo / medication / … |
| `0x09` | `subtype` | u8 (`ItemSubtype`) | |
| `0x0a` | `slot` | u8 (`ItemSlot`) | equip slot (e.g. hands) |
| `0x0c` | `icon` | `char*` | UI icon path (`.tga`) |
| `0x10` | `model` | `char*` | world model (`.ltb` — LithTech model) |
| `0x14` | `skin` | `char*` | texture (`.dtx` — LithTech texture) |
| `0x20` | `renderStyle` | `char*` | render style (`.ltb`) |
| `0x64` | `ammoType` | u16 (`ItemType`) | for weapons: the ammo item type |

The remaining ~30 fields (floats/uints at `0x24`, `0x28`–`0x5c`, `0x68`–`0x90`,
a `byte[8][3]` at `0x38`, two more `char*` at `0x5c`/`0x60`) are item stats /
attributes and asset refs — **names unverified** (RE-in-progress). Enum field
types (`ItemType`, `ItemCategory`, `ItemSlot`) carry `SCREAMING_SNAKE` wire
constants; the `item-table` skill maps a type id → its canonical name.

## Live-verified example — type 106

With the client running, dereferencing `g_ItemDefTable[106]` and decoding the
record (raw live-address reads via `tools/re/memory.py`):

```
g_ItemDefTable live @ 0x767a3fa8
  table[106] -> 0x766edff0
  &ItemDef_106 (live) = 0x766edff0   ; match: True
  ItemDef_106:
    type     = 106           # ITEM_TYPE_PORTABLE_VORTEX_PARTICLE_EMITTER
    category = 6             # ITEM_CATEGORY_MECHANICAL_AUGMENTATION
    subtype  = 0   slot = 10 # ITEM_SLOT_HANDS
    icon        -> 'Interface/Items/vorteximp.tga'
    model       -> 'Models/Items/vortex_implant.ltb'
    skin        -> 'Skins/Items/vortex_implant.dtx'
    renderStyle -> 'RS/default.ltb'
    ammoType = 0
```

The asset paths (`.ltb` model, `.dtx` texture) confirm the [[Client
Architecture|LithTech]] asset pipeline, and the type id / category / slot agree
with the `item-table` skill and the `ItemType`/`ItemCategory`/`ItemSlot` enums.

Reproduce: `fomre struct CShell.dll:0x1030dff0 /FOM/Types/Item/ItemDefinition`
for the field decode (string pointers must be followed through live memory, since
they hold runtime addresses into CShell's data section).

## Why it matters for the emulator

The table is the authoritative client-side item catalog: a server that needs to
validate item types, resolve ammo relationships, or interpret inventory packets
(`ID_MOVE_ITEMS`, `ID_ITEMS_ADDED`) can mirror these type→definition mappings.
See also the `item-table` skill for the committed type→name table.
