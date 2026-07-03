using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Core.Persistence;

namespace FOMServer.World.Core.Players
{
    internal class Player : IPersistable
    {
        private readonly Lock _syncRoot = new();

        private readonly Lock _currentUpdateLock = new();
        private WorldUpdate.CharacterUpdate _currentUpdate;

        // Authoritative in-memory inventory. Each entry carries the item plus its
        // placement (which container/slot it occupies); mutations raise
        // OnPersistableChange so the DB-backed persistence handler captures them,
        // which is what lets equipped gear survive a logout.
        private readonly List<PlacedItem> _inventory = new();

        public Player(uint id, int[]? initialAttributes = null)
        {
            Id = id;
            Attributes = new PlayerAttributes(this, initialAttributes);

            _currentUpdate.Id = id;
        }

        public event PersistableChangeCallback? OnPersistableChange;

        public uint Id { get; }

        public NetworkAddress Address { get; private set; } = NetworkAddress.Unassigned;

        public PlayerAttributes Attributes { get; }

        public void ClaimForClient(NetworkAddress address)
        {
            lock (_syncRoot)
            {
                if (Address != NetworkAddress.Unassigned)
                {
                    throw new InvalidOperationException($"Client '{address}' cannot claim player {Id} ({Address})");
                }
                Address = address;
            }
        }

        /// <summary>
        /// Adds an item to the player's backpack and signals the change for
        /// persistence. New items always land in the backpack; equipping is a
        /// subsequent <see cref="MoveItems"/>.
        /// </summary>
        public void AddItem(in Item item)
        {
            lock (_syncRoot)
            {
                _inventory.Add(new PlacedItem
                {
                    Item = item,
                    Container = ItemContainer.Inventory,
                    Slot = 0,
                });
            }

            OnPersistableChange?.Invoke(this);
        }

        /// <summary>
        /// Returns a snapshot copy of the player's items (all containers) for
        /// lookups by id, e.g. resolving the equipped weapon on fire/reload.
        /// </summary>
        public Item[] SnapshotInventory()
        {
            lock (_syncRoot)
            {
                var items = new Item[_inventory.Count];
                for (var i = 0; i < _inventory.Count; i++)
                {
                    items[i] = _inventory[i].Item;
                }

                return items;
            }
        }

        /// <summary>
        /// Returns a snapshot copy of the player's items with their placement, for
        /// persistence and for routing into the world-entry container arrays.
        /// </summary>
        public PlacedItem[] SnapshotPlacements()
        {
            lock (_syncRoot)
            {
                return _inventory.ToArray();
            }
        }

        /// <summary>
        /// Replaces the inventory with the given placed items (e.g. loaded from the
        /// database on world entry). Does <b>not</b> raise the persistence event —
        /// this is an authoritative load, not a change to persist back.
        /// </summary>
        public void LoadInventory(IEnumerable<PlacedItem> items)
        {
            lock (_syncRoot)
            {
                _inventory.Clear();
                _inventory.AddRange(items);
            }
        }

        /// <summary>
        /// Moves the items with the given instance ids into a container/slot in
        /// response to <c>ID_MOVE_ITEMS</c> (e.g. equipping gear), so the placement
        /// is persisted and restored on the next world entry. Returns whether any
        /// item was moved.
        /// </summary>
        public bool MoveItems(ReadOnlySpan<uint> ids, ItemContainer dest, byte destSlot)
        {
            var moved = false;
            lock (_syncRoot)
            {
                for (var i = 0; i < _inventory.Count; i++)
                {
                    if (!Contains(ids, _inventory[i].Item.Id))
                    {
                        continue;
                    }

                    var placed = _inventory[i];
                    placed.Container = dest;
                    placed.Slot = destSlot;
                    _inventory[i] = placed;
                    moved = true;
                }
            }

            if (moved)
            {
                OnPersistableChange?.Invoke(this);
            }

            return moved;
        }

        /// <summary>
        /// Sets the <see cref="ItemBase.Value"/> of the backpack item with the
        /// given instance id (e.g. a weapon's loaded rounds or a clip's remaining
        /// rounds) and returns the updated item. Returns false if no such item.
        /// </summary>
        public bool TrySetItemValue(uint itemId, ushort value, out Item updated)
        {
            updated = default;
            var changed = false;
            lock (_syncRoot)
            {
                for (var i = 0; i < _inventory.Count; i++)
                {
                    if (_inventory[i].Item.Id != itemId)
                    {
                        continue;
                    }

                    var placed = _inventory[i];
                    placed.Item.Base.Value = value;
                    _inventory[i] = placed;
                    updated = placed.Item;
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                OnPersistableChange?.Invoke(this);
            }

            return changed;
        }

        /// <summary>
        /// Removes the backpack item with the given instance id (e.g. a clip fully
        /// consumed by a reload). Returns false if no such item.
        /// </summary>
        public bool RemoveItem(uint itemId)
        {
            var removed = false;
            lock (_syncRoot)
            {
                for (var i = 0; i < _inventory.Count; i++)
                {
                    if (_inventory[i].Item.Id == itemId)
                    {
                        _inventory.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            }

            if (removed)
            {
                OnPersistableChange?.Invoke(this);
            }

            return removed;
        }

        public void ApplyUpdate(in WorldUpdate.PlayerUpdate update)
        {
            lock (_currentUpdateLock)
            {
                _currentUpdate = update.Character;
                _currentUpdate.Id = Id;
            }
        }

        public WorldUpdate.CharacterUpdate CaptureUpdate()
        {
            lock (_currentUpdateLock)
            {
                return _currentUpdate;
            }
        }

        private static bool Contains(ReadOnlySpan<uint> ids, uint id)
        {
            foreach (var candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
