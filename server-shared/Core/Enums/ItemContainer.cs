namespace FOMServer.Shared.Core.Enums
{
    /// <summary>
    /// Identifies which of the player's containers an item lives in. The values
    /// are the <c>src</c>/<c>dest</c> container ids the client sends in
    /// <c>ID_MOVE_ITEMS</c> (verified by RE of <c>CWindowInventory::OnEvent</c>)
    /// and line up 1:1 with the container arrays in
    /// <see cref="FOMServer.Shared.Core.Packets.RegisterClientReturn"/>.
    /// </summary>
    public enum ItemContainer : byte
    {
        Inventory = 1, // the backpack
        Equipment = 2, // worn gear; the slot is the ItemSlot value (Head..Shoes)
        Weapons = 3, // weapon slots; the slot is the 1-based weapon slot index
    }
}
