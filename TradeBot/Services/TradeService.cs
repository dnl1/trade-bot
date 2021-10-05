using TradeBot.Entities;
using TradeBot.Enums;
using TradeBot.Repositories;

namespace TradeBot.Services
{
    public class TradeService : ITradeService
    {
        private readonly ITradeRepository _tradeRepository;

        public TradeService(ITradeRepository tradeRepository)
        {
            _tradeRepository = tradeRepository;
        }

        public void SetOrdered(decimal altStartingBalance, decimal cryptoStartingBalance, double altTradeAmount)
        {
            var trade = new Trade(altStartingBalance, cryptoStartingBalance, altTradeAmount);

            _tradeRepository.Save(trade);
        }

        public void StartTradeLog(Coin origin, Coin target, Side side)
        {
            var trade = new Trade(origin, target, side);

            _tradeRepository.Save(trade);
        }
    }
}
