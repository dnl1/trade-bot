using System.Collections.Generic;
using Npgsql;
using TradeBot.Entities;
using TradeBot.Database;
using System.Linq;

namespace TradeBot.Repositories
{
    public class PairRepository : IPairRepository
    {
        private readonly IPostgresConnectionFactory _connectionFactory;

        public PairRepository(IPostgresConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Save(Pair pair)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO pairs (from_symbol, to_symbol, ratio)
                VALUES (@fromSymbol, @toSymbol, @ratio)
                ON CONFLICT (from_symbol, to_symbol)
                DO UPDATE SET ratio = EXCLUDED.ratio
                """;
            command.Parameters.AddWithValue("fromSymbol", pair.FromCoin.Symbol);
            command.Parameters.AddWithValue("toSymbol", pair.ToCoin.Symbol);
            command.Parameters.AddWithValue("ratio", pair.Ratio);
            command.ExecuteNonQuery();
        }

        public Pair? Get(string fromSymbol, string toSymbol)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT from_symbol, to_symbol, ratio
                FROM pairs
                WHERE from_symbol = @fromSymbol AND to_symbol = @toSymbol
                """;
            command.Parameters.AddWithValue("fromSymbol", fromSymbol);
            command.Parameters.AddWithValue("toSymbol", toSymbol);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapPair(reader) : null;
        }

        public IEnumerable<Pair> GetAll()
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT from_symbol, to_symbol, ratio
                FROM pairs
                ORDER BY from_symbol, to_symbol
                """;

            using var reader = command.ExecuteReader();
            var pairs = new List<Pair>();
            while (reader.Read())
            {
                pairs.Add(MapPair(reader));
            }

            return pairs;
        }

        public IEnumerable<Pair> GetPairsFrom(Coin currentCoin)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT from_symbol, to_symbol, ratio
                FROM pairs
                WHERE from_symbol = @fromSymbol
                ORDER BY to_symbol
                """;
            command.Parameters.AddWithValue("fromSymbol", currentCoin.Symbol);

            using var reader = command.ExecuteReader();
            var pairs = new List<Pair>();
            while (reader.Read())
            {
                pairs.Add(MapPair(reader));
            }

            return pairs;
        }

        public IEnumerable<Pair> GetPairsTo(Coin currentCoin)
        {
            using var connection = _connectionFactory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT from_symbol, to_symbol, ratio
                FROM pairs
                WHERE to_symbol = @toSymbol
                ORDER BY from_symbol
                """;
            command.Parameters.AddWithValue("toSymbol", currentCoin.Symbol);

            using var reader = command.ExecuteReader();
            var pairs = new List<Pair>();
            while (reader.Read())
            {
                pairs.Add(MapPair(reader));
            }

            return pairs;
        }

        private static Pair MapPair(NpgsqlDataReader reader)
        {
            return new Pair
            {
                FromCoin = new Coin(reader.GetString(0)),
                ToCoin = new Coin(reader.GetString(1)),
                Ratio = reader.GetDecimal(2)
            };
        }
    }
}
