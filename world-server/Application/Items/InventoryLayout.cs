using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.Items
{
    /// <summary>
    /// Routes a player's items into the world-entry container slots
    /// (<see cref="FOMServer.Shared.Core.Packets.RegisterClientReturn"/>'s
    /// equipment/weapon arrays and backpack) using each item's persisted
    /// <see cref="ItemContainer"/> and slot. Anything that doesn't belong in a
    /// slot — the backpack, an unmodelled container, an out-of-range slot, or a
    /// slot already taken — spills to the backpack so no item is ever dropped.
    /// </summary>
    internal static class InventoryLayout
    {
        /// <summary>
        /// Fills <paramref name="equipment"/>, <paramref name="weapons"/> and
        /// <paramref name="backpack"/> from <paramref name="items"/> and returns the
        /// number of backpack entries written. Slots are matched by instance id: an
        /// <see cref="Item.Id"/> of 0 marks an empty slot (a real id is never 0), so
        /// a collision spills to the backpack rather than overwriting.
        /// </summary>
        public static int Populate(
            Span<Item> equipment,
            Span<Item> weapons,
            Span<Item> backpack,
            ReadOnlySpan<PlacedItem> items)
        {
            var backpackCount = 0;
            foreach (var placed in items)
            {
                var equipIndex = placed.Slot - (int)ItemSlot.EquipmentStart;
                var weaponIndex = placed.Slot - 1;

                if (placed.Container == ItemContainer.Equipment
                    && equipIndex >= 0 && equipIndex < equipment.Length
                    && equipment[equipIndex].Id == 0)
                {
                    equipment[equipIndex] = placed.Item;
                }
                else if (placed.Container == ItemContainer.Weapons
                    && weaponIndex >= 0 && weaponIndex < weapons.Length
                    && weapons[weaponIndex].Id == 0)
                {
                    weapons[weaponIndex] = placed.Item;
                }
                else if (backpackCount < backpack.Length)
                {
                    backpack[backpackCount] = placed.Item;
                    backpackCount++;
                }
            }

            return backpackCount;
        }
    }
}
