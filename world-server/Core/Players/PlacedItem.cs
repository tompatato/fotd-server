using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;

namespace FOMServer.World.Core.Players
{
    /// <summary>
    /// An <see cref="Item"/> together with where it lives among the player's
    /// containers. The placement is what lets equipped gear survive a logout: it
    /// is persisted alongside the item state and replayed into the matching
    /// <see cref="FOMServer.Shared.Core.Packets.RegisterClientReturn"/> slot array
    /// on world entry. Without it every item would come back in the backpack.
    /// </summary>
    internal struct PlacedItem
    {
        public Item Item;
        public ItemContainer Container;
        public byte Slot;
    }
}
