using System.Collections.Generic;
using TradeBot.Entities;
using TradeBot.Database;
using TradeBot.Enums;

namespace TradeBot.Repositories
{
    internal class TradeRepository : ITradeRepository
    {
        private readonly IPostgresConnectionFactory _connectionFactory;

        public TradeRepository(IPostgresConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Save(Trade trade)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO trades (
                    alt_starting_balance,
                    crypto_starting_balance,
                    crypto_trading_amount,
                    alt_trade_amount,
                    state,
                    alt_coin_symbol,
                    crypto_coin_symbol,
                    side,
                    trade_date
                )
                VALUES (
                    @altStartingBalance,
                    @cryptoStartingBalance,
                    @cryptoTradingAmount,
                    @altTradeAmount,
                    @state,
                    @altCoinSymbol,
                    @cryptoCoinSymbol,
                    @side,
                    @tradeDate
                )
                """;
            command.Parameters.AddWithValue("altStartingBalance", (object?)trade.AltStartingBalance ?? DBNull.Value);
            command.Parameters.AddWithValue("cryptoStartingBalance", (object?)trade.CryptoStartingBalance ?? DBNull.Value);
            command.Parameters.AddWithValue("cryptoTradingAmount", trade.CryptoTradingAmount);
            command.Parameters.AddWithValue("altTradeAmount", (object?)trade.AltTradeAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("state", (int)trade.State);
            command.Parameters.AddWithValue("altCoinSymbol", (object?)trade.AltCoin?.Symbol ?? DBNull.Value);
            command.Parameters.AddWithValue("cryptoCoinSymbol", (object?)trade.CryptoCoin?.Symbol ?? DBNull.Value);
            command.Parameters.AddWithValue("side", trade.Side.HasValue ? (object)(int)trade.Side.Value : DBNull.Value);
            command.Parameters.AddWithValue("tradeDate", trade.Date);
            command.ExecuteNonQuery();
        }

        public IEnumerable<Trade> GetAll()
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT alt_starting_balance,
                       crypto_starting_balance,
                       crypto_trading_amount,
                       alt_trade_amount,
                       state,
                       alt_coin_symbol,
                       crypto_coin_symbol,
                       side
                FROM trades
                ORDER BY id
                """;

            using var reader = command.ExecuteReader();
            var trades = new List<Trade>();
            while (reader.Read())
            {
                trades.Add(MapTrade(reader));
            }

            return trades;
        }

        private static Trade MapTrade(Npgsql.NpgsqlDataReader reader)
        {
            var state = (TradeState)reader.GetInt32(4);

            if (!reader.IsDBNull(5) && !reader.IsDBNull(6) && !reader.IsDBNull(7))
            {
                return new Trade(
                    new Coin(reader.GetString(5)),
                    new Coin(reader.GetString(6)),
                    (Side)reader.GetInt32(7));
            }

            var trade = new Trade(
                reader.IsDBNull(0) ? 0m : reader.GetDecimal(0),
                reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                reader.IsDBNull(3) ? 0d : reader.GetDouble(3));

            if (state == TradeState.Complete)
                trade.SetComplete(reader.GetDecimal(2));

            return trade;
        }
    }
}
