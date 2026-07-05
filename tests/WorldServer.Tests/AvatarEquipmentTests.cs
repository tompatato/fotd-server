using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Application.Items;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Tests
{
    public class AvatarEquipmentTests
    {
        [Fact]
        public void EquippedGear_DressesMatchingAvatarSlots_WithItemType()
        {
            var avatar = new Avatar();

            AvatarEquipment.Apply(ref avatar,
            [
                Equipped(310, ItemSlot.Torso), // armour torso
                Equipped(610, ItemSlot.Shirt), // clothing shirt (distinct layer)
                Equipped(700, ItemSlot.Pants), // maps to the Bottoms field
                Equipped(500, ItemSlot.Shoes),
            ]);

            Assert.Equal((ushort)310, avatar.Torso);
            Assert.Equal((ushort)610, avatar.Shirt);
            Assert.Equal((ushort)700, avatar.Bottoms);
            Assert.Equal((ushort)500, avatar.Shoes);
        }

        [Fact]
        public void NonEquipmentItems_DoNotAffectAvatar()
        {
            var avatar = new Avatar();

            AvatarEquipment.Apply(ref avatar,
            [
                Placed(50, ItemContainer.Inventory, 0), // ammo in the backpack
                Placed(15, ItemContainer.Weapons, 1), // a weapon in a weapon slot
            ]);

            Assert.Equal((ushort)0, avatar.Torso);
            Assert.Equal((ushort)0, avatar.Shirt);
        }

        private static PlacedItem Equipped(ushort type, ItemSlot slot)
        {
            return Placed(type, ItemContainer.Equipment, (byte)slot);
        }

        private static PlacedItem Placed(ushort type, ItemContainer container, byte slot)
        {
            return new PlacedItem
            {
                Item = new Item { Id = type, Base = new ItemBase { Type = (ItemType)type } },
                Container = container,
                Slot = slot,
            };
        }
    }
}
