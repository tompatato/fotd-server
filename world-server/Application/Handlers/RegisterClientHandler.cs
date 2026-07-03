using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Handlers;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Packets;
using FOMServer.Shared.Core.Packets.Types;
using FOMServer.Shared.Core.Repositories;
using FOMServer.Shared.Metadata;
using FOMServer.World.Application.Items;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.Handlers
{
    [PacketHandler]
    internal class RegisterClientHandler : PacketHandlerBase<RegisterClient>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IItemRepository _itemRepository;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<RegisterClientHandler> _logger;

        public RegisterClientHandler(
            IPlayerRegistry playerRegistry,
            IItemRepository itemRepository,
            IClientPacketSender clientPacketSender,
            ILogger<RegisterClientHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _itemRepository = itemRepository;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in RegisterClient p)
        {
            var player = _playerRegistry.ClaimForClient(p.PlayerId, sender);
            if (player is null)
            {
                _logger.LogWarning("Client '{Sender}' attempted to register unexpected player {PlayerId}", sender, p.PlayerId);
                return;
            }

            // Load the player's persisted backpack so it survives across sessions.
            var persisted = _itemRepository.GetByPlayer(player.Id);
            player.LoadInventory(persisted.Select(ItemMapping.FromDto));

            using var response = new PacketWriter<RegisterClientReturn>(sender);
            ref var rData = ref response.Data;

            rData.WorldId = p.WorldId;
            rData.PlayerId = p.PlayerId;
            rData.Status = RegisterClientReturn.StatusCode.Success;

            // Placeholder face/hair; the persisted appearance (face/hair/sex/race
            // from the player row) is not loaded into the world server yet. The
            // clothing/armour slots are dressed from the equipped items below.
            rData.Avatar.Face = 5;
            rData.Avatar.Hair = 2;

            unsafe
            {
                rData.Attributes.Values[(int)AttributeType.Health] = 1000;
                rData.Attributes.Values[(int)AttributeType.Stamina] = 10000;
                rData.Attributes.Values[(int)AttributeType.MaxStamina] = 10000;
                rData.Attributes.Values[(int)AttributeType.StaminaRegeneration] = 600;
                rData.Attributes.Values[(int)AttributeType.JumpVelocityMultiplier] = 2000;
                rData.Attributes.Values[(int)AttributeType.Aura] = 1000;
                rData.Attributes.Values[(int)AttributeType.Agility] = 1200;
                rData.Attributes.Values[(int)AttributeType.SprintSpeedMultiplier] = 4000;
            }

            // Deliver the player's authoritative inventory (loaded from the DB
            // above, so it persists across sessions), routing each item into the
            // container slot that matches its persisted placement. This is what
            // makes equipped gear come back equipped rather than in the backpack.
            var placements = player.SnapshotPlacements();
            var backpackCount = InventoryLayout.Populate(
                rData.Equipment[..],
                rData.Weapons[..],
                rData.Inventory.Items[..],
                placements);
            rData.Inventory.ItemCount = (uint)backpackCount;

            // Dress the avatar from the equipped gear so the character renders
            // wearing it on spawn (the client builds its model from these slots).
            AvatarEquipment.Apply(ref rData.Avatar, placements);

            rData.Profile.PlayerName = "Naruto Uzumaki";
            rData.NodeId = 1;

            _clientPacketSender.Send(response.Build());
        }
    }
}
