using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.Strategies
{
    internal class DefaultStrategy : AutoTrader
    {
        private readonly ICoinRepository _coinRepository;
        private readonly ILogger _logger;
        private readonly BinanceApiManager _manager;
        private readonly AppSettings _appSettings;

        public DefaultStrategy(IPairRepository pairRepository, 
            ISnapshotRepository snapshotRepository, 
            ICoinRepository coinRepository,
            ILogger logger,
            BinanceApiManager manager,
            AppSettings appSettings) : base(pairRepository, snapshotRepository)
        {
            _coinRepository = coinRepository;
            _logger = logger;
            _manager = manager;
            _appSettings = appSettings;
        }

        public override void Initialize()
        {
            base.Initialize();
            InitializeCurrentCoin().Wait();
        }

        private async Task InitializeCurrentCoin()
        {
            var coin = _coinRepository.GetCurrent();

            if(null == coin)
            {
                string currentCoinSymbol = _appSettings.CurrentCoin;

                if (string.IsNullOrEmpty(currentCoinSymbol))
                {
                    var random = new Random();
                    int index = random.Next(_appSettings.Coins.Length);
                    currentCoinSymbol = _appSettings.Coins[index];
                }

                _logger.Information($"Setting initial coin to {currentCoinSymbol}");

                var currentCoin = new Coin
                {
                    Symbol = currentCoinSymbol,
                };

                _coinRepository.SaveCurrent(currentCoin);

                _logger.Information($"Purchasing {currentCoinSymbol} to begin trading");

                await _manager.BuyAlt(currentCoin, new Coin(_appSettings.Bridge));

                _logger.Information("Ready to start trading");
            }
        }
    }
}
