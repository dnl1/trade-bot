using System.Collections.Generic;
using Npgsql;
using TradeBot.Database;
using TradeBot.Entities;

namespace TradeBot.Repositories
{
    internal class CoinRepository : ICoinRepository
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private const string CURRENT_COIN = "CURRENT_COIN";

        public CoinRepository(IPostgresConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void SaveCurrent(Coin coin)
        {
            SaveCoin(CURRENT_COIN, coin.Symbol);
            SaveCoin(coin.Symbol, coin.Symbol);
        }

        public void Save(IEnumerable<string> coins)
        {
            foreach (var coin in coins)
            {
                SaveCoin(coin, coin);
            }
        }

        public Coin? GetCurrent()
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT symbol FROM coins WHERE key = @key";
            command.Parameters.AddWithValue("key", CURRENT_COIN);

            using var reader = command.ExecuteReader();
            return reader.Read() ? new Coin(reader.GetString(0)) : null;
        }

        private void SaveCoin(string key, string symbol)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO coins (key, symbol)
                VALUES (@key, @symbol)
                ON CONFLICT (key)
                DO UPDATE SET symbol = EXCLUDED.symbol
                """;
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("symbol", symbol);
            command.ExecuteNonQuery();
        }
    }
}
