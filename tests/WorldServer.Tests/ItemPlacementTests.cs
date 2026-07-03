using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Application.Items;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Tests
{
    public class ItemPlacementTests
    {
        private const uint PlayerId = 7;
        private const uint ItemId = 100;

        [Fact]
        public void MoveItems_ToEquipment_RecordsContainerAndSlot()
        {
            var player = new Player(PlayerId);
            player.AddItem(new Item { Id = ItemId });

            var moved = player.MoveItems([ItemId], ItemContainer.Equipment, (byte)ItemSlot.Torso);

            Assert.True(moved);
            var placed = Assert.Single(player.SnapshotPlacements());
            Assert.Equal(ItemContainer.Equipment, placed.Container);
            Assert.Equal((byte)ItemSlot.Torso, placed.Slot);
        }

        [Fact]
        public void MoveItems_UnknownId_DoesNotMove()
        {
            var player = new Player(PlayerId);
            player.AddItem(new Item { Id = ItemId });

            var moved = player.MoveItems([ItemId + 1], ItemContainer.Equipment, (byte)ItemSlot.Torso);

            Assert.False(moved);
            Assert.Equal(ItemContainer.Inventory, Assert.Single(player.SnapshotPlacements()).Container);
        }

        [Fact]
        public void AddItem_DefaultsToBackpack()
        {
            var player = new Player(PlayerId);
            player.AddItem(new Item { Id = ItemId });

            Assert.Equal(ItemContainer.Inventory, Assert.Single(player.SnapshotPlacements()).Container);
        }

        [Fact]
        public void EquippedItem_SurvivesPersistRoundTrip()
        {
            // Equip an item, persist it, then reload as on the next world entry.
            var player = new Player(PlayerId);
            player.AddItem(new Item { Id = ItemId });
            player.MoveItems([ItemId], ItemContainer.Equipment, (byte)ItemSlot.Head);

            var dtos = player.SnapshotPlacements()
                .Select(p => ItemMapping.ToDto(p, PlayerId))
                .ToList();

            var reloaded = new Player(PlayerId);
            reloaded.LoadInventory(dtos.Select(ItemMapping.FromDto));

            var placed = Assert.Single(reloaded.SnapshotPlacements());
            Assert.Equal(ItemContainer.Equipment, placed.Container);
            Assert.Equal((byte)ItemSlot.Head, placed.Slot);
            Assert.Equal(ItemId, placed.Item.Id);
        }
    }
}
