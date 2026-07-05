namespace FOMServer.Shared.Core.Dtos
{
    // Row of the `item` table. Property names match columns (snake_case) so Dapper
    // maps them directly, matching AccountDto/PlayerDto.
    public record ItemDto
    {
        public uint id { get; init; }

        public uint player_id { get; init; }

        public byte container { get; init; }

        public byte slot { get; init; }

        public ushort type { get; init; }

        public ushort value { get; init; }

        public ushort max_durability { get; init; }

        public ushort durability { get; init; }

        public byte durability_loss_factor { get; init; }

        public byte security { get; init; }

        public uint creator_player_id { get; init; }

        public uint timeout { get; init; }

        public uint stolen_from_player_id { get; init; }

        public byte classification { get; init; }

        public byte quality { get; init; }

        public byte attribute_bonus { get; init; }

        public uint balance_values { get; init; }
    }
}
