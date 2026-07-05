using FOMServer.Shared.Core.Repositories;
using FOMServer.World.Core.Items;

namespace FOMServer.World.Application.Items
{
    internal sealed class ItemInstanceIdGenerator : IItemInstanceIdGenerator
    {
        // Floor above the historical placeholder instance id (1001) so freshly
        // granted items never collide with legacy hardcoded world-entry items.
        private const long Floor = 100_000;

        private readonly IItemRepository _itemRepository;
        private readonly object _seedLock = new();
        private long _next = Floor;
        private bool _seeded;

        public ItemInstanceIdGenerator(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public uint Next()
        {
            EnsureSeeded();
            return (uint)Interlocked.Increment(ref _next);
        }

        // Seed lazily (on first allocation) from the persisted high-water mark so
        // ids stay unique across restarts. Deferred so the DB migration has surely
        // run by the time the first item is granted.
        private void EnsureSeeded()
        {
            if (_seeded)
            {
                return;
            }

            lock (_seedLock)
            {
                if (_seeded)
                {
                    return;
                }

                var maxPersisted = _itemRepository.GetMaxId();
                Interlocked.Exchange(ref _next, Math.Max(Floor, maxPersisted));
                _seeded = true;
            }
        }
    }
}
