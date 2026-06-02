using Npgsql;
using TradeBot.Settings;

namespace TradeBot.Database
{
    internal sealed class PostgresConnectionFactory : IPostgresConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresConnectionFactory(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
                throw new KeyNotFoundException("PostgresConnectionString not populated");

            _connectionString = settings.PostgresConnectionString;
        }

        public NpgsqlConnection OpenConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            return connection;
        }
    }
}
