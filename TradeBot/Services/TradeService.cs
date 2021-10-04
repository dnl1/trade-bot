using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var trade = new Trade
            {
                AltStartingBalance = altStartingBalance,
                CryptoStartingBalance = cryptoStartingBalance,
                AltTradeAmount = altTradeAmount,
                State = TradeState.Ordered
            };

            _tradeRepository.Save(trade);
        }

        public void StartTradeLog(Coin origin, Coin target, Side side)
        {
            var trade = new Trade(origin, target, side);

            _tradeRepository.Save(trade);
        }
    }
}
