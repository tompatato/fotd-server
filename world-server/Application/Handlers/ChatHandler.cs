using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;
using FOMServer.World.Core.WorldObjects;

namespace FOMServer.World.Application.Handlers
{
    [PacketHandler]
    internal class ChatHandler : PacketHandlerBase<Chat>
    {
        // Default item type for a bare "deploy" — a storage safe prop
        // (Models/Props/Terminals/safe.ltb), which has a real model. Other
        // renderable deployable props: 995 production terminal, 997 market
        // terminal, 996 repair unit, 36 wall turret. Pass one explicitly, e.g.
        // "deploy 995".
        private const ushort DefaultDeployType = 999;

        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly IWorldObjectService _worldObjectService;
        private readonly ILogger<ChatHandler> _logger;

        public ChatHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            IWorldObjectService worldObjectService,
            ILogger<ChatHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _worldObjectService = worldObjectService;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in Chat p)
        {
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received unexpected chat packet for player {PlayerId}", p.SenderId);
                return;
            }

            var message = p.Message;

            // Interim placement trigger: a plain chat word "deploy [itemType]"
            // places a world object at the player's position. NOT a slash command
            // — the client parses "/"-commands itself (see Game Master Commands
            // RE) and never sends unknown ones as chat, so the trigger is an
            // ordinary chat message. Stands in for the client's ID_DEPLOY_ITEM
            // path until that C->S request is wired.
            var parts = message?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (parts.Length > 0 && parts[0].Equals("deploy", StringComparison.OrdinalIgnoreCase))
            {
                var itemType = DefaultDeployType;
                if (parts.Length > 1 && ushort.TryParse(parts[1], out var parsed))
                {
                    itemType = parsed;
                }

                _logger.LogInformation(
                    "Player {PlayerId} issued deploy command '{Message}' (item type {ItemType})",
                    player.Id, message, itemType);
                _worldObjectService.Deploy(player, itemType);
                return;
            }

            using var response = new PacketWriter<Chat>(true, sender);
            ref var rData = ref response.Data;
            rData.Channel = p.Channel;
            rData.SenderId = p.SenderId;
            rData.SenderName = "Naruto Uzumaki";
            rData.Message = p.Message;
            _clientPacketSender.Broadcast(response.Build());
        }
    }
}
