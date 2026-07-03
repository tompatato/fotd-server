using FOMServer.Shared.Core.Dtos;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Packets.Types;

namespace FOMServer.World.Application.Items
{
    /// <summary>
    /// Converts between the wire/domain <see cref="Item"/> and the persisted
    /// <see cref="ItemDto"/>. The 4 <c>ItemBase.BalanceValues</c> bytes pack into a
    /// single little-endian u32 column.
    /// </summary>
    internal static class ItemMapping
    {
        // Backpack is the only container modelled so far.
        private const byte BackpackContainer = 0;

        public static unsafe ItemDto ToDto(Item item, uint playerId)
        {
            ref var b = ref item.Base;
            var balance = (uint)(b.BalanceValues[0]
                | (b.BalanceValues[1] << 8)
                | (b.BalanceValues[2] << 16)
                | (b.BalanceValues[3] << 24));

            return new ItemDto
            {
                id = item.Id,
                player_id = playerId,
                container = BackpackContainer,
                slot = 0,
                type = (ushort)b.Type,
                value = b.Value,
                max_durability = b.MaxDurability,
                durability = b.Durability,
                durability_loss_factor = b.DurabilityLossFactor,
                security = (byte)b.Security,
                creator_player_id = b.CreatorPlayerId,
                timeout = b.Timeout,
                stolen_from_player_id = b.StolenFromPlayerId,
                classification = b.Classification,
                quality = (byte)b.Quality,
                attribute_bonus = b.AttributeBonus,
                balance_values = balance,
            };
        }

        public static unsafe Item FromDto(ItemDto dto)
        {
            var item = new Item { Id = dto.id };
            ref var b = ref item.Base;
            b.Type = (ItemType)dto.type;
            b.Value = dto.value;
            b.MaxDurability = dto.max_durability;
            b.Durability = dto.durability;
            b.DurabilityLossFactor = dto.durability_loss_factor;
            b.Security = (ItemSecurity)dto.security;
            b.CreatorPlayerId = dto.creator_player_id;
            b.Timeout = dto.timeout;
            b.StolenFromPlayerId = dto.stolen_from_player_id;
            b.Classification = dto.classification;
            b.Quality = (ItemQuality)dto.quality;
            b.AttributeBonus = dto.attribute_bonus;
            b.BalanceValues[0] = (byte)(dto.balance_values & 0xFF);
            b.BalanceValues[1] = (byte)((dto.balance_values >> 8) & 0xFF);
            b.BalanceValues[2] = (byte)((dto.balance_values >> 16) & 0xFF);
            b.BalanceValues[3] = (byte)((dto.balance_values >> 24) & 0xFF);
            return item;
        }
    }
}
