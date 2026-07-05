using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Application.Handlers;
using FOMServer.World.Core;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;
using Microsoft.Extensions.Logging.Abstractions;

namespace FOMServer.World.Tests
{
    public class VortexGateHandlerTests
    {
        private const uint PlayerId = 42;
        private const WorldId RunningWorld = WorldId.Manhattan;

        private static readonly NetworkAddress ClientAddress =
            new() { BinaryAddress = 0x0200007F, Port = 61001 };

        [Fact]
        public void TravelRequest_ForRunningWorld_ApprovesWithRequestedNode()
        {
            var sender = new RecordingSender();
            var handler = CreateHandler(new Player(PlayerId), sender);

            handler.Handle(ClientAddress, new VortexGate
            {
                PlayerId = PlayerId,
                Type = VortexGateType.TravelRequest,
                World = RunningWorld,
                Node = 7,
            });

            var (id, approve) = Assert.Single(sender.Sent);
            Assert.Equal(PacketIdentifier.ID_VORTEX_GATE, id);
            Assert.Equal(VortexGateType.TravelApprove, approve.Type);
            Assert.Equal(PlayerId, approve.PlayerId);
            Assert.Equal(RunningWorld, approve.World);
            Assert.Equal(7, approve.Node);
        }

        [Fact]
        public void EnterGate_ApprovesTravelToPrimaryWorldAtDefaultNode()
        {
            var sender = new RecordingSender();
            var handler = CreateHandler(new Player(PlayerId), sender);

            // A physical gate's activation (ENTER) carries no chosen destination.
            handler.Handle(ClientAddress, new VortexGate
            {
                PlayerId = PlayerId,
                Type = VortexGateType.Enter,
            });

            var (id, approve) = Assert.Single(sender.Sent);
            Assert.Equal(PacketIdentifier.ID_VORTEX_GATE, id);
            Assert.Equal(VortexGateType.TravelApprove, approve.Type);
            Assert.Equal(RunningWorld, approve.World);
            Assert.Equal(1, approve.Node);
        }

        [Fact]
        public void TravelRequest_ForAnotherHostedWorld_IsHonoured()
        {
            var sender = new RecordingSender();
            var registry = new StubPlayerRegistry(new Player(PlayerId));
            var settings = new ServerSettings { WorldIds = [RunningWorld, WorldId.Apartments] };
            var handler = new VortexGateHandler(registry, sender, settings, NullLogger<VortexGateHandler>.Instance);

            handler.Handle(ClientAddress, new VortexGate
            {
                PlayerId = PlayerId,
                Type = VortexGateType.TravelRequest,
                World = WorldId.Apartments,
                Node = 2,
            });

            // The server hosts Apartments, so travel there is authorised as-is.
            var (_, approve) = Assert.Single(sender.Sent);
            Assert.Equal(WorldId.Apartments, approve.World);
            Assert.Equal(2, approve.Node);
        }

        [Fact]
        public void TravelRequest_ForUnhostedWorld_RedirectsToPrimaryWorld()
        {
            var sender = new RecordingSender();
            var handler = CreateHandler(new Player(PlayerId), sender);

            // The client obeys the world echoed in the approve, so redirecting an
            // unhosted destination to the primary world keeps single-server travel
            // working instead of stalling on an offline world.
            handler.Handle(ClientAddress, new VortexGate
            {
                PlayerId = PlayerId,
                Type = VortexGateType.TravelRequest,
                World = WorldId.Apartments,
                Node = 3,
            });

            var (_, approve) = Assert.Single(sender.Sent);
            Assert.Equal(RunningWorld, approve.World);
            Assert.Equal(3, approve.Node);
        }

        [Fact]
        public void TravelRequest_FromUnregisteredClient_IsDropped()
        {
            var sender = new RecordingSender();
            var handler = CreateHandler(null, sender);

            handler.Handle(ClientAddress, new VortexGate
            {
                PlayerId = PlayerId,
                Type = VortexGateType.TravelRequest,
                World = RunningWorld,
            });

            Assert.Empty(sender.Sent);
        }

        [Fact]
        public void TravelRequest_ForForeignPlayerId_IsDropped()
        {
            var sender = new RecordingSender();
            var handler = CreateHandler(new Player(PlayerId), sender);

            handler.Handle(ClientAddress, new VortexGate
            {
                PlayerId = PlayerId + 1,
                Type = VortexGateType.TravelRequest,
                World = RunningWorld,
            });

            Assert.Empty(sender.Sent);
        }

        private static VortexGateHandler CreateHandler(Player? player, RecordingSender sender)
        {
            var registry = new StubPlayerRegistry(player);
            var settings = new ServerSettings { WorldIds = [RunningWorld] };
            return new VortexGateHandler(registry, sender, settings, NullLogger<VortexGateHandler>.Instance);
        }

        private sealed class StubPlayerRegistry : IPlayerRegistry
        {
            private readonly Player? _player;

            public StubPlayerRegistry(Player? player)
            {
                _player = player;
            }

            public Player? Get(uint playerId) => _player is not null && _player.Id == playerId ? _player : null;

            public Player? Get(NetworkAddress address) => _player;

            public IEnumerable<Player> GetAll() => _player is null ? [] : [_player];

            public Player PrepareForClient(uint playerId, uint clientBinaryAddress) => throw new NotSupportedException();

            public Player? ClaimForClient(uint playerId, NetworkAddress sender) => throw new NotSupportedException();

            public void Logout(Player player) => throw new NotSupportedException();
        }

        private sealed class RecordingSender : IClientPacketSender
        {
            public List<(PacketIdentifier Id, VortexGate Packet)> Sent { get; } = [];

            public void Send(in QueuePacket packet)
            {
                Sent.Add((packet.Id, MemoryMarshal.Read<VortexGate>(packet.Data)));
                packet.Release();
            }

            public void Broadcast(in QueuePacket packet) => packet.Release();

            public void Disconnect(in NetworkAddress address) { }
        }
    }
}
