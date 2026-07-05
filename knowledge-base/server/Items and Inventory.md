# Items and Inventory

How the **world server** models a player's items, grants them, mutates them
(ammo), and persists them. The wire formats and client behaviour are documented
in the **client** vault (*Inventory*, *Game Master Commands*, *Weapons and Ammo*,
*Item Definitions* over there); this note is the server-side counterpart.

## The backpack model — `Player`

Each `Player`
([`world-server/Core/Players/Player.cs`](../../world-server/Core/Players/Player.cs))
holds an in-memory backpack (`List<Item>`) and is `IPersistable`. Mutations raise
`OnPersistableChange`, which the persistence layer batches into a database sync
(below):

| Method | Use |
| --- | --- |
| `AddItem` | grant an item (e.g. `/spawn`) |
| `TrySetItemValue(id, value)` | change an item's `value` in place (loaded rounds, clip count) |
| `RemoveItem(id)` | drop an item (a consumed clip) |
| `SnapshotInventory()` | copy for packet building |
| `LoadInventory(items)` | authoritative load on world entry — does **not** raise the persist event |

`Item` = `{ uint Id; ItemBase Base }`; the durable, non-zero instance `Id` comes
from `IItemInstanceIdGenerator`
([`world-server/Application/Items`](../../world-server/Application/Items)), which
seeds itself from the persisted high-water mark so ids never collide across
restarts.

## Granting items — `/spawn`

`GamemasterHandler` handles `ID_GAMEMASTER` (179). For the spawn command it grants
`quantity` copies of the requested item (fresh instance ids), floors a weapon's
loaded rounds to ≥ 1, and replies with `ID_ITEMS_ADDED` (`dest = 1`, the backpack).
GM access is currently only gated client-side (see [[Server Topology]] on the
temporary `accountType = 22` login unlock).

## Ammo — fire & reload

Server-authoritative, because the client waits on the server for ammo changes:

- **Fire** (`WeaponFireHandler`, `ID_WEAPONFIRE`) — decrements the weapon's loaded
  `value` (floor 0) and replies `ID_ITEMS_CHANGED`.
- **Reload** (`ReloadHandler`, `ID_RELOAD`) — finds a clip whose type matches the
  weapon's ammo type, loads one clip as a fresh magazine, removes the clip, and
  replies `ID_ITEMS_CHANGED` (weapon) + `ID_ITEMS_REMOVED` (clip, `dest = 3`).

`IItemCatalog` (`ItemCatalog`/`ItemCatalogData`, generated from the client item
tables — see the `item-table` skill) classifies weapon vs ammunition and resolves
each weapon's ammo type. Note `ID_ITEMS_CHANGED` is category-gated client-side and
cannot update *ammo* items, which is why clips are removed rather than decremented.

## Persistence

The backpack is DB-backed so it survives sessions and restarts:

- **Schema** — the `item` table (`CreateItem` migration; run by the master, shared
  MariaDB — see [[Server Topology]]): durable `id` PK, `player_id` FK, container/
  slot, and the `ItemBase` fields (`balance_values` packs the 4 tuning bytes into a
  u32).
- **Repository** — `IItemRepository`/`DbItemRepository` (Dapper): `GetByPlayer`,
  `ReplaceForPlayer` (whole-inventory delete + insert in one transaction),
  `GetMaxId`.
- **Sync** — `PlayerInventoryPersistenceHandler` (an `IPersistenceHandler` for
  `Player`) runs on every dirty-flag, writing the whole backpack. The
  `PersistenceService` batches rapid changes (e.g. firing) into far fewer writes.
- **Load** — `RegisterClientHandler` loads the player's items from the DB on world
  entry (`ItemMapping` converts `ItemDto` ↔ `Item`) and delivers them in
  `ID_REGISTER_CLIENT_RETURN`.

## Known gaps

- **No equipment-slot model** — "the weapon" is the first weapon in the backpack;
  multi-weapon loadouts need equip tracking via `ID_MOVE_ITEMS`.
- **No magazine-capacity field** yet, so one clip == one full magazine (a partial
  reload discards leftover rounds).
- **Logout** to the login screen isn't implemented (see [[Server Topology]]); items
  do persist regardless (the disconnect path still syncs).

See [[Server Topology]] (ports, DB, login/handoff) and the **client** vault's
*Inventory* / *Game Master Commands* / *Weapons and Ammo* for the wire side.
