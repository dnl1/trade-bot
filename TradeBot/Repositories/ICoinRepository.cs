using TradeBot.Entities;

namespace TradeBot.Repositories
{
    public interface ICoinRepository
    {
        Coin GetCurrent();
        void SaveCurrent(Coin coin);
    }
}