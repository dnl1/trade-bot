using System;
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
        private readonly CandleAggregator _candleAggregator;
        private CancellationTokenSource? _lifetimeCts;
        private CountdownEvent? _countdown;

        public MarketDataListenerService(
            AppSettings settings,
            BinanceStreamManager binanceStreamManager,
            ILogger logger,
            ISnapshotRepository repository,
            CandleAggregator candleAggregator)
        {
            _settings             = settings;
            _binanceStreamManager = binanceStreamManager;
            _logger               = logger;
            _snapshotRepository   = repository;
            _candleAggregator     = candleAggregator;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            StartProcessor(_lifetimeCts.Token);
            SubscribeTickers(_lifetimeCts.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
            return Task.CompletedTask;
        }

        private void StartProcessor(CancellationToken cancellationToken)
        {
            _ = _binanceStreamManager.StreamProcessor(cancellationToken)
                    .ContinueWith(
                        t => _logger.Error($"StreamProcessor stopped unexpectedly: {t.Exception?.Flatten().InnerException}"),
                        TaskContinuationOptions.OnlyOnFaulted);
        }

        private void SubscribeTickers(CancellationToken cancellationToken)
        {
            var tickers = _settings.Coins.Select(coin => $"{coin}{_settings.Bridge}".ToUpper()).ToList();

            _countdown = new CountdownEvent(tickers.Count);
            // ConcurrentDictionary + TryUpdate ensures each symbol signals exactly once,
            // even if two aggTrade messages arrive before the first callback completes.
            var seen = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>(
                tickers.Select(t => new KeyValuePair<string, bool>(t, false)));
            tickers.ForEach(t => _logger.Info($"Subscribing for {t}"));

            _binanceStreamManager.Subscribe<Snapshot>(tickers, "aggTrade", snapshot =>
            {
                _snapshotRepository.Save(snapshot);
                _candleAggregator.Process(snapshot);

                // TryUpdate atomically flips false→true; only succeeds once per symbol
                if (!seen.TryUpdate(snapshot.Symbol, true, false)) return;
                _logger.Debug($"Loaded {snapshot.Symbol}");
                _countdown.Signal();
            }, cancellationToken);
        }

        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (_countdown is null) return true;
            return _countdown.Wait(timeout, cancellationToken);
        }
    }
}
