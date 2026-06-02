using System;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using Microsoft.Extensions.Hosting;
using TradeBot.Factories;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.HostedServices
{
    internal class TradingService : IHostedService
    {
        private static readonly TimeSpan InitialSnapshotTimeout = TimeSpan.FromMinutes(2);
        private readonly AppSettings _settings;
        private readonly MarketDataListenerService _marketDataListenerService;
        private readonly StrategyFactory _strategyFactory;
        private readonly ICoinRepository _coinRepository;
        private readonly ILogger _logger;

        public TradingService(AppSettings settings,
            MarketDataListenerService marketDataListenerService,
            StrategyFactory strategyFactory,
            ICoinRepository coinRepository,
            ILogger logger)
        {
            _settings = settings;
            _marketDataListenerService = marketDataListenerService;
            _strategyFactory = strategyFactory;
            _coinRepository = coinRepository;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting");

            _coinRepository.Save(_settings.Coins);

            await _marketDataListenerService.StartAsync(cancellationToken);

            var strategy = _strategyFactory.GetStrategy(_settings.Strategy);

            if (null == strategy)
                throw new InvalidOperationException($"Invalid strategy name: '{_settings.Strategy}'");

            _logger.Info($"Chosen strategy: {_settings.Strategy}");

            _logger.Info("Waiting for snapshots to load");
            if (!_marketDataListenerService.Wait(InitialSnapshotTimeout, cancellationToken))
                throw new TimeoutException(
                    $"Timed out after {InitialSnapshotTimeout.TotalSeconds:0}s waiting for initial " +
                    $"snapshots for: {string.Join(", ", _settings.Coins)}");
            _logger.Info("Snapshots loaded");

            await strategy.Initialize();
            await strategy.Scout();

            // SemaphoreSlim guards against overlapping Scout() invocations.
            // The semaphore is acquired INSIDE Task.Run (not before) so that if
            // Task.Run is cancelled the finally block still runs and releases it.
            var scoutLock = new SemaphoreSlim(1, 1);

            JobManager.AddJob(() =>
            {
                // Fire-and-forget inside a proper Task so exceptions are caught.
                // Cancellation handled inside the lambda so finally always runs.
                _ = Task.Run(async () =>
                {
                    if (!await scoutLock.WaitAsync(0, CancellationToken.None))
                    {
                        _logger.Warn("[Scout] Previous tick still running — skipping this interval");
                        return;
                    }
                    using var timeoutCts = new CancellationTokenSource(
                        TimeSpan.FromMinutes(_settings.ScoutTimeoutMinutes));
                    try
                    {
                        var scout = strategy.Scout();
                        var timeout = Task.Delay(Timeout.Infinite, timeoutCts.Token);
                        if (await Task.WhenAny(scout, timeout) == timeout)
                        {
                            _logger.Error($"[Scout] Timed out after {_settings.ScoutTimeoutMinutes} min — releasing lock");
                            return; // finally still runs → lock released
                        }
                        await scout; // re-observe any exception
                    }
                    catch (OperationCanceledException) { /* host shutting down */ }
                    catch (Exception ex)
                    {
                        _logger.Error($"[Scout] Unhandled exception: {ex}");
                    }
                    finally
                    {
                        scoutLock.Release();
                    }
                }, CancellationToken.None); // CancellationToken.None so the Task itself isn't aborted mid-work

            }, s => s.WithName("scouting").ToRunEvery(_settings.ScoutSleepTime).Minutes());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            JobManager.StopAndBlock();
            return _marketDataListenerService.StopAsync();
        }
    }
}
