using FluentMigrator;
using FOMServer.Shared.Extensions;

namespace FOMServer.Shared.Infrastructure.Migrations.Item
{
    [Migration(202607031200, "Creates the item table.")]
    public class CreateItem : ForwardOnlyMigration
    {
        public override void Up()
        {
            // `id` is the durable item instance id — assigned by the server's id
            // generator, not auto-increment. `balance_values` packs ItemBase's
            // 4 tuning bytes little-endian into a u32.
            Create.Table("item")
                .WithColumn("id").AsUInt32().NotNullable().PrimaryKey()
                .WithColumn("player_id").AsUInt32().NotNullable().ForeignKey("player", "id")
                .WithColumn("container").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("slot").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("type").AsUInt16().NotNullable()
                .WithColumn("value").AsUInt16().NotNullable().WithDefaultValue(0)
                .WithColumn("max_durability").AsUInt16().NotNullable().WithDefaultValue(0)
                .WithColumn("durability").AsUInt16().NotNullable().WithDefaultValue(0)
                .WithColumn("durability_loss_factor").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("security").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("creator_player_id").AsUInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("timeout").AsUInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("stolen_from_player_id").AsUInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("classification").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("quality").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("attribute_bonus").AsUInt8().NotNullable().WithDefaultValue(0)
                .WithColumn("balance_values").AsUInt32().NotNullable().WithDefaultValue(0);

            Create.Index("ix_item_player_id")
                .OnTable("item")
                .OnColumn("player_id").Ascending();
        }
    }
}
