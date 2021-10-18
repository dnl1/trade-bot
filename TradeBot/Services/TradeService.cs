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

        public void SetComplete(Trade trade, decimal cummulativeQuoteQty)
        {
            trade.SetComplete(cummulativeQuoteQty);

            _tradeRepository.Save(trade);

        }

        public Trade SetOrdered(decimal altStartingBalance, decimal cryptoStartingBalance, double altTradeAmount)
        {
            var trade = new Trade(altStartingBalance, cryptoStartingBalance, altTradeAmount);

            _tradeRepository.Save(trade);

            return trade;
        }

        public void StartTradeLog(Coin origin, Coin target, Side side)
        {
            var trade = new Trade(origin, target, side);

            _tradeRepository.Save(trade);
        }
    }
}
