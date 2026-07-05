using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.World.Application.Handlers;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;
using Microsoft.Extensions.Logging.Abstractions;

namespace FOMServer.World.Tests
{
    public class CheckMailHandlerTests
    {
        private const uint PlayerId = 42;

        private static readonly NetworkAddress ClientAddress =
            new() { BinaryAddress = 0x0200007F, Port = 61001 };

        [Fact]
        public void CheckMail_RepliesWithEmptyInboxForRegisteredPlayer()
        {
            var sender = new RecordingSender();
            var registry = new StubPlayerRegistry(new Player(PlayerId));
            var handler = new CheckMailHandler(registry, sender, NullLogger<CheckMailHandler>.Instance);

            handler.Handle(ClientAddress, new CheckMail { PlayerId = PlayerId });

            var (id, mail) = Assert.Single(sender.Sent);
            Assert.Equal(PacketIdentifier.ID_MAIL, id);
            Assert.Equal(PlayerId, mail.PlayerId);
            Assert.Equal(0, mail.MailCount);
        }

        [Fact]
        public void CheckMail_FromUnregisteredClient_IsDropped()
        {
            var sender = new RecordingSender();
            var registry = new StubPlayerRegistry(null);
            var handler = new CheckMailHandler(registry, sender, NullLogger<CheckMailHandler>.Instance);

            handler.Handle(ClientAddress, new CheckMail { PlayerId = PlayerId });

            Assert.Empty(sender.Sent);
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
            public List<(PacketIdentifier Id, Mail Packet)> Sent { get; } = [];

            public void Send(in QueuePacket packet)
            {
                Sent.Add((packet.Id, MemoryMarshal.Read<Mail>(packet.Data)));
                packet.Release();
            }

            public void Broadcast(in QueuePacket packet) => packet.Release();

            public void Disconnect(in NetworkAddress address) { }
        }
    }
}
