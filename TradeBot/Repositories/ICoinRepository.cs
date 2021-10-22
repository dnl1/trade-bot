using TradeBot.Entities;

namespace TradeBot.Repositories
{
    public interface ICoinRepository
    {
        Coin GetCurrent();
        void Save(IEnumerable<string> coins);
        void SaveCurrent(Coin coin);
    }
}