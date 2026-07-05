using System.Collections.Concurrent;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Core.WorldObjects;

namespace FOMServer.World.Application.WorldObjects
{
    /// <summary>
    /// In-memory store of placed world objects, keyed by their assigned instance
    /// id. Ids are process-unique and monotonic so a placement always has a
    /// distinct <c>SWO(category)_id</c> on the client. Not persisted yet — the
    /// set is rebuilt as objects are (re)placed.
    /// </summary>
    internal sealed class WorldObjectRegistry : IWorldObjectRegistry
    {
        private readonly ConcurrentDictionary<uint, PlacedObject> _objects = new();
        private uint _nextId;

        public PlacedObject Add(ushort category, ushort itemType, PositionRotation position, uint extra)
        {
            var id = Interlocked.Increment(ref _nextId);
            var placed = new PlacedObject(
                category,
                new WorldObject
                {
                    Id = id,
                    Type = itemType,
                    State = 0,
                    Extra = extra,
                    Position = position,
                });

            _objects[id] = placed;
            return placed;
        }

        public IReadOnlyList<PlacedObject> Snapshot() => _objects.Values.ToArray();
    }
}
