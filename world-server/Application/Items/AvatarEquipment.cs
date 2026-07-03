using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.Items
{
    /// <summary>
    /// Projects a player's equipped gear onto the clothing/armour slots of the
    /// wire <see cref="Avatar"/> so the character renders wearing it. The client
    /// dresses its model from these slot values — each is an item-type id resolved
    /// against the item-definition table (verified in <c>CPlayerObj::UpdateAvatar</c>,
    /// <c>Object.lto</c> rva <c>0x17de0</c>) — so a zeroed Avatar leaves the player
    /// looking undressed even with items in the equipment container.
    /// </summary>
    internal static class AvatarEquipment
    {
        public static void Apply(ref Avatar avatar, ReadOnlySpan<PlacedItem> items)
        {
            foreach (var placed in items)
            {
                if (placed.Container != ItemContainer.Equipment)
                {
                    continue;
                }

                var type = (ushort)placed.Item.Base.Type;
                switch ((ItemSlot)placed.Slot)
                {
                    case ItemSlot.Head: avatar.Head = type; break;
                    case ItemSlot.Eyes: avatar.Eyes = type; break;
                    case ItemSlot.Shoulders: avatar.Shoulder = type; break;
                    case ItemSlot.Torso: avatar.Torso = type; break;
                    case ItemSlot.Arms: avatar.Arms = type; break;
                    case ItemSlot.Hands: avatar.Hands = type; break;
                    case ItemSlot.Legs: avatar.Legs = type; break;
                    case ItemSlot.Back: avatar.Back = type; break;
                    case ItemSlot.Hat: avatar.Hat = type; break;
                    case ItemSlot.Shirt: avatar.Shirt = type; break;
                    case ItemSlot.Pants: avatar.Bottoms = type; break;
                    case ItemSlot.Shoes: avatar.Shoes = type; break;
                }
            }
        }
    }
}
