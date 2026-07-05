using FOMServer.Master.Application.Handlers;
using FOMServer.Master.Application.Players;
using FOMServer.Master.Core.Networking;
using FOMServer.Master.Core.Players;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.RakNet;
using FOMServer.Shared.Core.Packets.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace FOMServer.Master.Tests
{
    public class WorldLogoutHandlerTests
    {
        private const uint PlayerId = 42;
        private static readonly WorldId World = WorldId.Manhattan;

        [Fact]
        public void WorldLogout_WhenInWorld_ForwardsToWorldAndLeavesWorld()
        {
            var fixture = new Fixture();
            var session = fixture.EnterWorld();

            fixture.Handler.Handle(fixture.ClientAddress, new WorldLogout { PlayerId = PlayerId });

            // The logout is handed to the world server hosting the player, and
            // the session is no longer in any world.
            Assert.Contains(PacketHelpers.GetPacketTypeId<WorldLogout>(), fixture.WorldSender.Sent);
            Assert.Null(session.CurrentWorld);
        }

        [Fact]
        public void WorldLogout_WhenChangingWorlds_IsNoOp()
        {
            var fixture = new Fixture();
            var session = fixture.EnterWorld();

            fixture.Handler.Handle(
                fixture.ClientAddress,
                new WorldLogout { PlayerId = PlayerId, IsChangingWorlds = true });

            // A world switch is driven by the follow-up ID_WORLD_LOGIN, so this
            // notification does nothing on its own: nothing forwarded, still in-world.
            Assert.Empty(fixture.WorldSender.Sent);
            Assert.Equal(World, session.CurrentWorld);
        }

        [Fact]
        public void WorldLogout_ForForeignPlayerId_IsDropped()
        {
            var fixture = new Fixture();
            fixture.EnterWorld();

            fixture.Handler.Handle(fixture.ClientAddress, new WorldLogout { PlayerId = PlayerId + 1 });

            Assert.Empty(fixture.WorldSender.Sent);
        }

        private sealed class Fixture
        {
            public Fixture()
            {
                ClientRegistry = new ClientRegistry();
                WorldSender = new RecordingWorldPacketSender();

                Handler = new WorldLogoutHandler(
                    WorldSender,
                    ClientRegistry,
                    new SingleWorldRegistry(),
                    NullLogger<WorldLogoutHandler>.Instance);

                NewConnection = new NewIncomingConnectionHandler(
                    ClientRegistry, NullLogger<NewIncomingConnectionHandler>.Instance);
            }

            public NetworkAddress ClientAddress { get; } =
                new() { BinaryAddress = 0x0100007F, Port = 7777 };

            public ClientRegistry ClientRegistry { get; }

            public RecordingWorldPacketSender WorldSender { get; }

            public WorldLogoutHandler Handler { get; }

            public NewIncomingConnectionHandler NewConnection { get; }

            /// <summary>Registers a session and drives it into <see cref="World"/>.</summary>
            public ClientSession EnterWorld()
            {
                NewConnection.Handle(ClientAddress, new NewIncomingConnection());
                var session = ClientRegistry.Get(ClientAddress)!;
                session.BeginLogin(PlayerId);
                session.BeginWorldTransfer(World);
                session.CompleteWorldTransfer();
                return session;
            }
        }

        private sealed class SingleWorldRegistry : IWorldServerRegistry
        {
            private readonly WorldServer _server = new()
            {
                ServerAddress = new NetworkAddress { BinaryAddress = 0x0200007F, Port = 61001 },
                PublicAddress = new NetworkAddress { BinaryAddress = 0x0200007F, Port = 61001 }
            };

            public WorldServer[] GetAll() => [_server];

            public WorldServer? Get(WorldId id) => id == World ? _server : null;

            public WorldId[] Register(WorldId[] ids, NetworkAddress serverAddress, NetworkAddress publicAddress) => [];

            public WorldId[] Unregister(NetworkAddress serverAddress) => [];
        }

        private sealed class RecordingWorldPacketSender : IWorldPacketSender
        {
            public List<PacketIdentifier> Sent { get; } = [];

            public void Send(in QueuePacket packet)
            {
                Sent.Add(packet.Id);
                packet.Release();
            }

            public void Broadcast(in QueuePacket packet)
            {
                Sent.Add(packet.Id);
                packet.Release();
            }
        }
    }
}
