using Dapper;
using FOMServer.Shared.Core.Dtos;
using FOMServer.Shared.Core.Repositories;

namespace FOMServer.Shared.Infrastructure.Repositories
{
    internal class DbItemRepository : IItemRepository
    {
        private const string Columns =
            "`id`, `player_id`, `container`, `slot`, `type`, `value`, `max_durability`, " +
            "`durability`, `durability_loss_factor`, `security`, `creator_player_id`, " +
            "`timeout`, `stolen_from_player_id`, `classification`, `quality`, " +
            "`attribute_bonus`, `balance_values`";

        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DbItemRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public IReadOnlyList<ItemDto> GetByPlayer(uint playerId)
        {
            using var connection = _dbConnectionFactory.Create();

            return connection.Query<ItemDto>(
                $"SELECT {Columns} FROM `item` WHERE `player_id` = @playerId",
                new { playerId }
            ).AsList();
        }

        public void ReplaceForPlayer(uint playerId, IReadOnlyList<ItemDto> items)
        {
            using var connection = _dbConnectionFactory.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            connection.Execute(
                "DELETE FROM `item` WHERE `player_id` = @playerId",
                new { playerId },
                transaction
            );

            if (items.Count > 0)
            {
                connection.Execute(
                    $@"INSERT INTO `item` ({Columns}) VALUES (
                        @id, @player_id, @container, @slot, @type, @value, @max_durability,
                        @durability, @durability_loss_factor, @security, @creator_player_id,
                        @timeout, @stolen_from_player_id, @classification, @quality,
                        @attribute_bonus, @balance_values)",
                    items,
                    transaction
                );
            }

            transaction.Commit();
        }

        public uint GetMaxId()
        {
            using var connection = _dbConnectionFactory.Create();

            return connection.ExecuteScalar<uint?>("SELECT MAX(`id`) FROM `item`") ?? 0;
        }
    }
}
