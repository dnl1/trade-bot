using TradeBot.Entities;

namespace TradeBot.Repositories
{
    internal interface ICoinRepository
    {
        Coin GetCurrent();
        void SaveCurrent(Coin coin);
    }
}