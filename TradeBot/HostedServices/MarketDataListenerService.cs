using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Models;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.HostedServices
{
    internal class MarketDataListenerService
    {
        private readonly AppSettings _settings;
        private readonly BinanceStreamManager _binanceStreamManager;
        private readonly ILogger _logger;
        private readonly ISnapshotRepository _snapshotRepository;
        private CountdownEvent _countdown;

        public MarketDataListenerService(AppSettings settings, BinanceStreamManager binanceStreamManager, ILogger logger, ISnapshotRepository repository)
        {
            _settings = settings;
            _binanceStreamManager = binanceStreamManager;
            _logger = logger;
            _snapshotRepository = repository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var tickers = _settings.Coins.Select(coin => $"{coin}{_settings.Bridge}".ToUpper()).ToList();

            int total = tickers.Count;
            _countdown = new CountdownEvent(total);
            var dict = new Dictionary<string, bool>();

            tickers.ForEach(ticker =>
            {
                _logger.Info($"Subscribing for {ticker}");
                dict.Add(ticker, true);
            });

            _binanceStreamManager.StreamProcessor().ConfigureAwait(false);

            _binanceStreamManager.Subscribe<Snapshot>(tickers, "aggTrade", (snapshot) =>
            {
                _snapshotRepository.Save(snapshot);

                if (dict[snapshot.Symbol])
                {
                    _countdown.Signal();
                    dict[snapshot.Symbol] = false;
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public CountdownEvent GetCountdownEvent() => _countdown;
    }
}
