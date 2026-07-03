using FOMServer.Shared.Core.Dtos;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Core.Repositories;
using FOMServer.World.Application.Items;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Infrastructure.Persistence
{
    /// <summary>
    /// Persists a <see cref="Player"/>'s inventory — items and their container/slot
    /// placement — to the database whenever the player is marked dirty, as a
    /// whole-inventory sync (the repository deletes the player's rows and reinserts
    /// the current set in one transaction).
    /// </summary>
    internal sealed class PlayerInventoryPersistenceHandler : IPersistenceHandler
    {
        private readonly IItemRepository _itemRepository;

        public PlayerInventoryPersistenceHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public Type EntityType => typeof(Player);

        public Task PersistAsync(IPersistable entity)
        {
            if (entity is not Player player)
            {
                throw new InvalidOperationException(
                    $"{nameof(PlayerInventoryPersistenceHandler)} cannot persist {entity.GetType().Name}");
            }

            var items = player.SnapshotPlacements();
            var rows = new List<ItemDto>(items.Length);
            foreach (var item in items)
            {
                rows.Add(ItemMapping.ToDto(item, player.Id));
            }

            _itemRepository.ReplaceForPlayer(player.Id, rows);
            return Task.CompletedTask;
        }
    }
}
