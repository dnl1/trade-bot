using System.Collections.Generic;
using TradeBot.Models;
using TradeBot.Database;

namespace TradeBot.Repositories
{
    internal class SnapshotRepository : ISnapshotRepository
    {
        private readonly IPostgresConnectionFactory _connectionFactory;

        public SnapshotRepository(IPostgresConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Save(Snapshot snapshot)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO snapshots (
                    symbol, event_type, event_time, agg_trade_id, price, quantity,
                    first_trade_id, last_trade_id, trade_time, is_market_maker, ignore_flag
                )
                VALUES (
                    @symbol, @eventType, @eventTime, @aggTradeId, @price, @quantity,
                    @firstTradeId, @lastTradeId, @tradeTime, @isMarketMaker, @ignoreFlag
                )
                ON CONFLICT (symbol)
                DO UPDATE SET
                    event_type = EXCLUDED.event_type,
                    event_time = EXCLUDED.event_time,
                    agg_trade_id = EXCLUDED.agg_trade_id,
                    price = EXCLUDED.price,
                    quantity = EXCLUDED.quantity,
                    first_trade_id = EXCLUDED.first_trade_id,
                    last_trade_id = EXCLUDED.last_trade_id,
                    trade_time = EXCLUDED.trade_time,
                    is_market_maker = EXCLUDED.is_market_maker,
                    ignore_flag = EXCLUDED.ignore_flag
                """;
            command.Parameters.AddWithValue("symbol", snapshot.Symbol);
            command.Parameters.AddWithValue("eventType", snapshot.EventType);
            command.Parameters.AddWithValue("eventTime", snapshot.EventTime);
            command.Parameters.AddWithValue("aggTradeId", snapshot.AggTradeId);
            command.Parameters.AddWithValue("price", snapshot.Price);
            command.Parameters.AddWithValue("quantity", snapshot.Quantity);
            command.Parameters.AddWithValue("firstTradeId", snapshot.FirstTradeId);
            command.Parameters.AddWithValue("lastTradeId", snapshot.LastTradeId);
            command.Parameters.AddWithValue("tradeTime", snapshot.TradeTime);
            command.Parameters.AddWithValue("isMarketMaker", snapshot.IsMarketMaker);
            command.Parameters.AddWithValue("ignoreFlag", snapshot.Ignore);
            command.ExecuteNonQuery();
        }

        public Snapshot? Get(string symbol)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT symbol, event_type, event_time, agg_trade_id, price, quantity,
                       first_trade_id, last_trade_id, trade_time, is_market_maker, ignore_flag
                FROM snapshots
                WHERE symbol = @symbol
                """;
            command.Parameters.AddWithValue("symbol", symbol);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapSnapshot(reader) : null;
        }

        public IEnumerable<Snapshot> GetAll()
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT symbol, event_type, event_time, agg_trade_id, price, quantity,
                       first_trade_id, last_trade_id, trade_time, is_market_maker, ignore_flag
                FROM snapshots
                ORDER BY symbol
                """;

            using var reader = command.ExecuteReader();
            var snapshots = new List<Snapshot>();
            while (reader.Read())
            {
                snapshots.Add(MapSnapshot(reader));
            }

            return snapshots;
        }

        private static Snapshot MapSnapshot(Npgsql.NpgsqlDataReader reader)
        {
            return new Snapshot
            {
                Symbol = reader.GetString(0),
                EventType = reader.GetString(1),
                EventTime = reader.GetInt64(2),
                AggTradeId = reader.GetInt64(3),
                Price = reader.GetDecimal(4),
                Quantity = reader.GetString(5),
                FirstTradeId = reader.GetInt64(6),
                LastTradeId = reader.GetInt64(7),
                TradeTime = reader.GetInt64(8),
                IsMarketMaker = reader.GetBoolean(9),
                Ignore = reader.GetBoolean(10)
            };
        }
    }
}
