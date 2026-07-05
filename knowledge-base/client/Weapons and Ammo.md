# Weapons and Ammo

How weapons hold ammo, how firing/reloading is expected to flow, and what the
server must do to make ammo behave. This is the model behind the `/spawn`
observation that a spawned weapon "has one bullet and firing doesn't deplete it".
Status: the **data model and the server→client update packet are confirmed**; the
two client→server packets (`ID_WEAPONFIRE`, `ID_RELOAD`) still need their exact
wire formats captured (they are not symbol-named — see "Open" below). All
addresses are `CShell.dll` RVAs (image base `0x10000000`).

## Data model

- A weapon's **loaded round count is `ItemBase.value`** (field 2 of [[Inventory|`ItemBase`]],
  16-bit). A freshly `/spawn`ed weapon comes across the wire with `value = 0`
  (empty); the server's spawn handler floors it to 1, which is why a spawned gun
  shows exactly "1 bullet".
- **Ammo clips are separate inventory items** — `ItemCategory.AMMUNITION` (2),
  distinct from `ItemCategory.WEAPON` (3). See the `item-table` skill.
- A weapon's definition links it to its ammo: `ItemDefinition.ammoType` (offset
  `0x64`, a `u16` item type) names the ammo item type that reloads it. See
  [[Item Definitions]] (`g_ItemDefTable[type]`).
- Weapon subtypes (`ItemSubtype`) distinguish handguns/rifles/energy/etc.; ammo
  is matched by `ammoType`, not subtype.

## Expected flow (server-authoritative)

`ID_WEAPONFIRE` (135) and `ID_RELOAD` (145) are **client→server** — they are *not*
cases in the client's receive dispatcher `FOM::HandlePacket` (rva `0x199a40`), so
the client does not process them coming back. Depletion is therefore driven by the
server: the client sends the action up, the server mutates the weapon's `value`
and pushes the new state down via **`ID_ITEMS_CHANGED` (130)**. Our world server
currently has no handler for 135/145, so nothing depletes.

```
fire:    client --ID_WEAPONFIRE--> server : server decrements weapon.value,
              server --ID_ITEMS_CHANGED--> client (weapon, new value)
reload:  client --ID_RELOAD-->     server : find clip item (type == weapon.ammoType),
              consume clip, set weapon.value = clip capacity,
              server --ID_ITEMS_CHANGED--> client (weapon) + remove/decrement clip
```

> Not yet verified whether the client *also* predicts depletion locally. The
> observed "firing doesn't deplete at all" points to server-authoritative (a
> local prediction would deplete regardless of the server), but confirm by
> capturing whether the client sends `ID_WEAPONFIRE` per shot (see Open).

## `ID_ITEMS_CHANGED` (130) — the ammo-update packet (confirmed)

Client read `FUN_10190990`, applied by `HandlePacket` case handler `FUN_10190b90`.
Wire format (every integer RakNet-compressed unless noted):

| # | Field | Enc | Notes |
| --- | --- | --- | --- |
| 1 | `playerId` | u32 | must equal the local player's id or the packet is dropped |
| 2 | `count` | u8 | number of changed items |
| 3… | `count × Item` | — | each `Item` = `id` (u32) + `ItemBase` |

Each `Item` is read by `FUN_102550a0` — the read-twin of the `/spawn` item writer
`FUN_10255040` (u32 id + `ItemBase::Read`). So `ID_ITEMS_CHANGED` reuses the exact
[[Inventory|`Item`/`ItemBase`]] encoding, but as a **flat `u8 count` + `Item[]`**
(no `dest` byte, no `ItemList` stack grouping — unlike [[Inventory|`ID_ITEMS_ADDED`]]).

The handler (`FUN_10190b90`) matches each item by instance id, updates it in place
in `PLAYERDATA_INVENTORY`, and shows a `±N` change popup where N is
`newValue - oldValue` (`FUN_10133e70`). So sending the weapon back with a lower
`value` both updates the HUD ammo and shows the delta.

> **Category-gated (important).** `FUN_10190b90` switches on the item's *category*
> (`FUN_102330f0` reads `ItemDefinition+0x08`) and only acts on categories
> **3 (weapon), 5 (armor), 6 (mech-aug), 16–19 (materials/loot/tools)**. Every
> other category — including **2 (ammunition)** — hits the `default` and is a
> no-op. So `ID_ITEMS_CHANGED` can update a *weapon's* loaded rounds but **cannot
> update or show a change to an ammo/clip item**; clip changes must go through
> `ID_ITEMS_REMOVED` instead.

## `ID_ITEMS_REMOVED` (129) — removing clips (confirmed)

Client handler `FUN_10192d40`: `playerId` (compressed u32), `dest` (u8), then an
**id set** (`FUN_1023d7b0`: compressed u16 count + that many compressed u32 ids).
`dest` selects the removal path — and they are **not** equivalent:

| `dest` | Behaviour |
| --- | --- |
| `1` | per-id **global** lookup (`FUN_1018d350`/`FUN_10158cf0`) that zeroes the record + shows a chat line, but **does not cover backpack items and never refreshes** — a no-op for a backpack clip |
| `3` | `GetPlayerData(PLAYERDATA_INVENTORY)` → `FUN_1023f120` **splices the ids out of the backpack container** → sets the dirty flag (**refreshes**) |
| `6` | inventory + terminal-chemical-lab context |

So removing a backpack clip requires **`dest = 3`** (verified: `dest = 1` silently
did nothing).

## `ID_UNLOAD_WEAPON` (143) — inverse of reload (partial)

Handled client-side by `FUN_1018ea20`. A sub-code byte (`local_7c`) selects the
outcome (`2` = apply the unload to inventory, `3` = show a "can't" message). Useful
as a cross-check on the weapon↔clip↔value relationship when implementing reload.

## Captured payloads (live probe, 2026-07-03)

Registered temporary raw-capture readers for 135/145 and logged the payload
(byte-aligned, after the id byte) from a live session: spawn pistol (type 1) +
9mm ammo (type 50), equip, reload, fire ×5, reload. Ammo spawned with
**`value = 57`** — so an ammo item's `value` is its round count (a full clip),
and it is *not* floored (already ≥ 1).

```
ID_RELOAD      40 bits  : 00 00 00 00 C0
ID_RELOAD     104 bits  : 00 00 00 00 80 00 30 D4 20 00 18 6A 20
ID_WEAPONFIRE  88 bits  : 00 00 00 00 80 00 40 00 00 06 C0   (shot 1)
ID_WEAPONFIRE  88 bits  : 00 00 00 00 80 00 40 00 00 06 E0   (shot 2)
ID_WEAPONFIRE  88 bits  : 00 00 00 00 80 00 40 00 00 07 00   (shot 3)
ID_WEAPONFIRE  88 bits  : 00 00 00 00 80 00 40 00 00 07 20   (shot 4)
ID_WEAPONFIRE  88 bits  : 00 00 00 00 80 00 40 00 00 07 40   (shot 5)
```

Structural observations (field-level decode still pending — see Open):
- **Each reload sends a pair**: one 40-bit packet then a 104-bit packet
  (identical across both reload actions).
- **Fire packets are fixed except a trailing counter** that steps by a constant
  amount per shot (`…06C0 → 06E0 → 0700 → 0720 → 0740`) — a shot sequence number
  or client tick.
- All start with **32 zero bits**, which is *not* a compressed small `playerId`
  (`playerId=1` compresses to `0xF1`, leading `1`-bits). So the leading field is
  either raw or a non-obvious value — decode needs RakNet's real `ReadCompressed`
  (a hand bit-walk was inconclusive), or the client's send function.

> Implication for implementation: firing/reload do **not** obviously carry the
> weapon instance id, so a server-authoritative depletion needs the server to
> know the player's *equipped* weapon — i.e. a server-side inventory/equipment
> model (equip happens via `ID_MOVE_ITEMS`, which the server currently only
> echoes). That model is the larger prerequisite, bigger than the packet decode.

## Server implementation (working, 2026-07-03)

Server-authoritative ammo is implemented and verified live (fire depletes,
reload refills from a clip; the client's ammo HUD tracks the server):

- **Item catalog** — `world-server` `IItemCatalog` / `ItemCatalogData`
  (generated from the `item-table` skill: the 39 weapons/ammo items with each
  weapon's `ammoType`). Classifies inventory items as weapon vs ammunition.
- **`ID_ITEMS_CHANGED` (130)** — outbound serializer implemented to the decoded
  format (`playerId` u32, `u8 count`, `count × Item`), reusing `ItemSerializer`.
- **`WeaponFireHandler`** — on `ID_WEAPONFIRE`, finds the player's weapon,
  `value -= 1` (floor 0), replies `ID_ITEMS_CHANGED`. Payload ignored.
- **`ReloadHandler`** — on `ID_RELOAD`, finds a clip whose type == the weapon's
  `ammoType`, loads **one clip as a fresh magazine** (`weapon.value = clip.value`),
  removes that clip (`Player.RemoveItem`), and replies `ID_ITEMS_CHANGED` (weapon)
  + **`ID_ITEMS_REMOVED` (129) with `dest = 3`** (the backpack container-splice
  path — `dest = 1` does not touch backpack items). A reload when already full is
  answered with an `ID_ITEMS_CHANGED` re-sync (no clip consumed) rather than
  silence.
- **`ID_WEAPONFIRE` / `ID_RELOAD`** inbound readers are opaque capture structs
  (payload not parsed — the server picks the weapon/clip itself).
- `Player.TrySetItemValue(id, value)` / `Player.RemoveItem(id)` mutate the backpack
  in place (session-only; deliberately do not raise the persistence event).

v1 simplifications (documented in the handlers): "the weapon" is the first
weapon-category backpack item (no equipment-slot model yet — needs `ID_MOVE_ITEMS`
equip tracking); one clip == one full magazine (no per-weapon capacity field), so a
reload of a partial mag discards the leftover rounds.

**Known accepted quirk:** firing *during the reload animation* pops the client's
"Reload failed: you don't have ammunition" message even though the reload
succeeds. Confirmed **client-side** — the server log shows no rejected/skipped
reload when it appears, so the client generates it from a fire input mid-reload
(the clip being loaded is transiently unavailable). No server lever; accepted.

## Open

- **Clip capacity / rounds-per-clip field** in `ItemDefinition` — one of the
  still-unnamed fields (`0x24`, `0x28`–`0x5c`, `0x68`–`0x90`); needed so reload
  tops up to a real magazine size (and keeps a partial mag's rounds) instead of
  one-clip-per-mag.
- **Equipment model** — track the equipped weapon (via `ID_MOVE_ITEMS` into the
  weapon slot) so multi-weapon loadouts deplete the right gun.
- **`ID_WEAPONFIRE` / `ID_RELOAD` field decode** — payloads captured but not
  decoded (not needed for the current server-authoritative model).

## Reproduce

```bash
fomre decompile "CShell.dll:0x10199a40"   # FOM::HandlePacket dispatcher (no 135/145 cases)
fomre decompile "CShell.dll:0x10190990"   # ID_ITEMS_CHANGED read (playerId, u8 count, Item[])
fomre decompile "CShell.dll:0x10190b90"   # ID_ITEMS_CHANGED apply (category-gated in-place update)
fomre decompile "CShell.dll:0x102330f0"   # category router (reads ItemDefinition+0x08)
fomre decompile "CShell.dll:0x10192d40"   # ID_ITEMS_REMOVED handler (dest 1 vs 3 vs 6)
fomre decompile "CShell.dll:0x1023f120"   # dest==3 backpack container splice
fomre decompile "CShell.dll:0x102550a0"   # Item read (u32 id + ItemBase::Read)
fomre decompile "CShell.dll:0x1018ea20"   # ID_UNLOAD_WEAPON handler
fomre type /FOM/Types/Item/ItemDefinition # ammoType @ 0x64, category/subtype
```

See [[Inventory]] (`Item`/`ItemBase` encoding, `ID_ITEMS_ADDED`), [[Item Definitions]]
(`ammoType`, the catalog), and [[Game Master Commands]] (`/spawn`, which sets the
initial `value`).
