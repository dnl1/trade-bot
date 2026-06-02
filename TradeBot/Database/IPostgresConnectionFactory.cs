using Npgsql;

namespace TradeBot.Database
{
    public interface IPostgresConnectionFactory
    {
        NpgsqlConnection OpenConnection();
    }
}
