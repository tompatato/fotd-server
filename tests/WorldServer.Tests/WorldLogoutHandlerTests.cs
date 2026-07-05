using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Application.Handlers;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;
using Microsoft.Extensions.Logging.Abstractions;

namespace FOMServer.World.Tests
{
    public class WorldLogoutHandlerTests
    {
        private const uint PlayerId = 42;

        private static readonly NetworkAddress MasterAddress =
            new() { BinaryAddress = 0x0100007F, Port = 5000 };

        private static readonly NetworkAddress ClientAddress =
            new() { BinaryAddress = 0x0200007F, Port = 61001 };

        [Fact]
        public void WorldLogout_ClosesClientConnectionAndLogsOut()
        {
            var player = new Player(PlayerId);
            player.ClaimForClient(ClientAddress);

            var registry = new StubPlayerRegistry(player);
            var sender = new DisconnectRecordingSender();
            var handler = new WorldLogoutHandler(registry, sender, NullLogger<WorldLogoutHandler>.Instance);

            handler.Handle(MasterAddress, new WorldLogout { PlayerId = PlayerId });

            // The client's world connection is closed (its cue to leave "Logging
            // Out") and the player is torn down from the world.
            Assert.Contains(ClientAddress, sender.Disconnected);
            Assert.Contains(player, registry.LoggedOut);
        }

        [Fact]
        public void WorldLogout_ForUnknownPlayer_IsDropped()
        {
            var registry = new StubPlayerRegistry(null);
            var sender = new DisconnectRecordingSender();
            var handler = new WorldLogoutHandler(registry, sender, NullLogger<WorldLogoutHandler>.Instance);

            handler.Handle(MasterAddress, new WorldLogout { PlayerId = PlayerId });

            Assert.Empty(sender.Disconnected);
            Assert.Empty(registry.LoggedOut);
        }

        private sealed class StubPlayerRegistry : IPlayerRegistry
        {
            private readonly Player? _player;

            public StubPlayerRegistry(Player? player)
            {
                _player = player;
            }

            public List<Player> LoggedOut { get; } = [];

            public Player? Get(uint playerId) => _player is not null && _player.Id == playerId ? _player : null;

            public Player? Get(NetworkAddress address) => _player;

            public IEnumerable<Player> GetAll() => _player is null ? [] : [_player];

            public Player PrepareForClient(uint playerId, uint clientBinaryAddress) => throw new NotSupportedException();

            public Player? ClaimForClient(uint playerId, NetworkAddress sender) => throw new NotSupportedException();

            public void Logout(Player player) => LoggedOut.Add(player);
        }

        private sealed class DisconnectRecordingSender : IClientPacketSender
        {
            public List<NetworkAddress> Disconnected { get; } = [];

            public void Send(in QueuePacket packet) => packet.Release();

            public void Broadcast(in QueuePacket packet) => packet.Release();

            public void Disconnect(in NetworkAddress address) => Disconnected.Add(address);
        }
    }
}
