using FOMServer.Shared.Core.Dtos;

namespace FOMServer.Shared.Core.Repositories
{
    public interface IItemRepository
    {
        /// <summary>Returns all items owned by the given player.</summary>
        IReadOnlyList<ItemDto> GetByPlayer(uint playerId);

        /// <summary>
        /// Replaces the player's item rows with the given set (delete-all +
        /// insert, in one transaction) — a whole-inventory sync.
        /// </summary>
        void ReplaceForPlayer(uint playerId, IReadOnlyList<ItemDto> items);

        /// <summary>Highest item instance id in use, or 0 if the table is empty.</summary>
        uint GetMaxId();
    }
}
