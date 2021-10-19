
namespace TradeBot.Database
{
    public interface ICacher
    {
        T Execute<T>(Func<T> impl, TimeSpan ttl);
        Task<T> ExecuteAsync<T>(Func<Task<T>> impl, TimeSpan ttl);
    }
}