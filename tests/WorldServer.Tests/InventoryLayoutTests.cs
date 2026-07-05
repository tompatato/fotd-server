using FOMServer.Shared.Core.Constants;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Application.Items;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Tests
{
    public class InventoryLayoutTests
    {
        private const int EquipmentSlots = (int)ItemSlot.EquipmentEnd - (int)ItemSlot.EquipmentStart;

        [Fact]
        public void EquipmentItem_LandsInItsSlot_NotBackpack()
        {
            var layout = new Layout();

            var count = layout.Populate([Placed(10, ItemContainer.Equipment, (byte)ItemSlot.Torso)]);

            Assert.Equal(0, count);
            Assert.Equal(10u, layout.Equipment[(int)ItemSlot.Torso - (int)ItemSlot.EquipmentStart].Id);
        }

        [Fact]
        public void WeaponItem_LandsInSlotIndexMinusOne()
        {
            var layout = new Layout();

            var count = layout.Populate([Placed(20, ItemContainer.Weapons, 2)]);

            Assert.Equal(0, count);
            Assert.Equal(20u, layout.Weapons[1].Id);
        }

        [Fact]
        public void BackpackItem_LandsInBackpack()
        {
            var layout = new Layout();

            var count = layout.Populate([Placed(30, ItemContainer.Inventory, 0)]);

            Assert.Equal(1, count);
            Assert.Equal(30u, layout.Backpack[0].Id);
        }

        [Fact]
        public void SlotCollision_SpillsLoserToBackpack()
        {
            var layout = new Layout();

            var count = layout.Populate(
            [
                Placed(40, ItemContainer.Equipment, (byte)ItemSlot.Head),
                Placed(41, ItemContainer.Equipment, (byte)ItemSlot.Head),
            ]);

            // First keeps the slot; the collision spills to the backpack rather than
            // overwriting (which would silently lose an item).
            Assert.Equal(40u, layout.Equipment[0].Id);
            Assert.Equal(1, count);
            Assert.Equal(41u, layout.Backpack[0].Id);
        }

        [Fact]
        public void OutOfRangeOrUnmodelledPlacement_SpillsToBackpack()
        {
            var layout = new Layout();

            var count = layout.Populate(
            [
                Placed(50, ItemContainer.Equipment, 99), // slot past the equipment array
                Placed(51, ItemContainer.Weapons, 0), // weapon slot 0 is out of range
                Placed(52, (ItemContainer)9, 0), // container we don't model
            ]);

            Assert.Equal(3, count);
            Assert.Equal(new[] { 50u, 51u, 52u }, new[] { layout.Backpack[0].Id, layout.Backpack[1].Id, layout.Backpack[2].Id });
        }

        private static PlacedItem Placed(uint id, ItemContainer container, byte slot)
        {
            return new PlacedItem
            {
                Item = new Item { Id = id },
                Container = container,
                Slot = slot,
            };
        }

        // Backing arrays that stand in for the RegisterClientReturn container slots,
        // which are too large to allocate directly in a test.
        private sealed class Layout
        {
            public Item[] Equipment { get; } = new Item[EquipmentSlots];

            public Item[] Weapons { get; } = new Item[PlayerConstants.NumWeaponSlots];

            public Item[] Backpack { get; } = new Item[16];

            public int Populate(PlacedItem[] items)
            {
                return InventoryLayout.Populate(Equipment, Weapons, Backpack, items);
            }
        }
    }
}
