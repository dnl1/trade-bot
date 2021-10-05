using TradeBot.Entities;
using TradeBot.Enums;

namespace TradeBot.Services
{
    public interface ITradeService
    {
        void SetOrdered(decimal altStartingBalance, decimal cryptoStartingBalance, double altTradeAmount);
        void StartTradeLog(Coin origin, Coin target, Side bUY);
    }
}