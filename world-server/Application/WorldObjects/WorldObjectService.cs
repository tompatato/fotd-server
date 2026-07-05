using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;
using FOMServer.World.Core.WorldObjects;
using WorldObjectsPacket = FOMServer.Shared.Core.Packets.WorldObjects;

namespace FOMServer.World.Application.WorldObjects
{
    /// <summary>
    /// Builds and dispatches <c>ID_WORLD_OBJECTS</c> placements. Single-category
    /// for now: everything is placed under <see cref="DefaultCategory"/>, the
    /// generic static-world-object category the client renders via its item
    /// model. See knowledge-base/client/World Objects.md.
    /// </summary>
    internal sealed class WorldObjectService : IWorldObjectService
    {
        // Generic deployable category (0x1fa) — the client spawns these as
        // StaticWorldObjects, using the ItemType's model.
        private const ushort DefaultCategory = 0x1fa;

        private readonly IWorldObjectRegistry _registry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<WorldObjectService> _logger;

        public WorldObjectService(
            IWorldObjectRegistry registry,
            IClientPacketSender clientPacketSender,
            ILogger<WorldObjectService> logger)
        {
            _registry = registry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public PlacedObject Deploy(Player player, ushort itemType)
        {
            var position = player.CaptureUpdate().Position;
            var placed = _registry.Add(DefaultCategory, itemType, position, player.Id);

            // Broadcast the new object to everyone (including the deployer, who
            // needs to see what they just placed).
            SendCategory(placed.Category, new[] { placed.Object }, destination: null);

            _logger.LogInformation(
                "Player {PlayerId} deployed object {ObjectId} (item type {ItemType}) at ({X},{Y},{Z}) rot {Rot}",
                player.Id, placed.Object.Id, itemType,
                position.Pos.X, position.Pos.Y, position.Pos.Z, position.Rot);

            return placed;
        }

        public void SendExistingTo(in NetworkAddress address)
        {
            var snapshot = _registry.Snapshot();
            if (snapshot.Count == 0)
            {
                return;
            }

            // The packet carries one category per send, so group placements by
            // category and send each group to the newly-entered client.
            foreach (var group in snapshot.GroupBy(o => o.Category))
            {
                var objects = group.Select(o => o.Object).ToArray();
                SendCategory(group.Key, objects, address);
            }
        }

        private void SendCategory(ushort category, WorldObject[] objects, NetworkAddress? destination)
        {
            // A single packet holds at most MaxWorldObjects records; chunk larger
            // sets across multiple category updates.
            for (var offset = 0; offset < objects.Length; offset += WorldObjectsPacket.MaxWorldObjects)
            {
                var chunk = Math.Min(WorldObjectsPacket.MaxWorldObjects, objects.Length - offset);

                using var writer = destination is null
                    ? new PacketWriter<WorldObjectsPacket>()
                    : new PacketWriter<WorldObjectsPacket>(destination.Value);

                ref var data = ref writer.Data;
                data.SubType = WorldObjectUpdate.Category;
                data.Category = category;
                data.Count = (ushort)chunk;
                for (var i = 0; i < chunk; i++)
                {
                    data.Objects[i] = objects[offset + i];
                }

                if (destination is null)
                {
                    _clientPacketSender.Broadcast(writer.Build());
                }
                else
                {
                    _clientPacketSender.Send(writer.Build());
                }
            }
        }
    }
}
