using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Entities;
using static TradeBot.Indicators.Indicators;
using TradeBot.Models;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.Strategies
{
    /// <summary>
    /// Mean-reversion strategy using Bollinger Bands with ADX, RSI, ATR and fee filters.
    ///
    /// Entry (all must be true):
    ///   - Expected return to middle band > BbMinProfitAboveFees (fee-adjusted minimum)
    ///   - Price below lower band  (%B &lt; 0)
    ///   - RSI(14) &lt; RsiOversold (default 30)
    ///   - ATR / ATR-SMA &lt; AtrVolatilityMultiplier (default 1.5)
    ///   - ADX(14) &lt; AdxThreshold (default 25)
    ///   - Bandwidth &gt; BbMinBandwidth (default 4%)
    ///
    /// Exit:
    ///   - Exchange STOP_LOSS_LIMIT order fills (immediate, no polling gap)
    ///   - OR price reaches middle band (SMA) → cancel stop order, sell at limit
    /// </summary>
    internal class BollingerBandsStrategy : AutoTrader
    {
        private readonly ICandleRepository _candleRepository;
        private readonly AppSettings       _settings;
        private readonly BinanceApiManager _manager;
        private readonly ICoinRepository   _coinRepository;
        private readonly ILogger           _logger;
        // AtrSma requires atrPeriod(14) + smaPeriod(20) + 1 = 35 minimum; 50 gives better convergence
        private const int WarmupCandles = 50;

        // Active position state
        private Coin?  _position;
        private decimal _entryPrice;
        private decimal _entryAtr;
        private long    _stopOrderId;
        private decimal _positionQty;

        public BollingerBandsStrategy(
            IPairRepository    pairRepository,
            ISnapshotRepository snapshotRepository,
            ICoinRepository    coinRepository,
            ILogger            logger,
            BinanceApiManager  manager,
            AppSettings        settings,
            ICandleRepository  candleRepository)
            : base(pairRepository, snapshotRepository, settings, manager, logger, coinRepository)
        {
            _candleRepository = candleRepository;
            _settings         = settings;
            _manager          = manager;
            _coinRepository   = coinRepository;
            _logger           = logger;
        }

        public override Task Initialize()
        {
            _logger.Info("[BB] Strategy ready. Waiting for candle warm-up...");
            return Task.CompletedTask;
        }

        public override async Task Scout()
        {
            if (_position is not null)
            {
                // Check if exchange stop-loss already filled
                if (_stopOrderId > 0 && await StopOrderFilled())
                    return;

                // Check candle-based exit (target or polling stop)
                var exitCandles = Candles(_position.Symbol);
                if (!ShouldExit(exitCandles))
                    return;

                await ExitPosition();
                // Fall through to scan for new entry after exit
            }

            // Scan all coins for the strongest buy signal
            Coin?   bestCoin = null;
            decimal bestPctB = 0m;
            decimal bestAtr  = 0m;

            foreach (var symbol in _settings.Coins)
            {
                var candles = Candles(symbol);
                if (candles.Count < WarmupCandles)
                {
                    _logger.Debug($"[BB] {symbol}: warming up ({candles.Count}/{WarmupCandles} candles)");
                    continue;
                }

                var bb     = BollingerBands(candles, _settings.BbPeriod, _settings.BbStdDev);
                var rsi    = Rsi(candles, _settings.RsiPeriod);
                var atr    = Atr(candles);
                var atrSma = AtrSma(candles);
                var adx    = Adx(candles);

                if (bb is null || rsi is null || atr is null || atrSma is null || adx is null) continue;

                decimal currentPrice  = candles[^1].Close;
                decimal atrRatio      = atrSma > 0 ? atr.Value / atrSma.Value : 0m;

                // Expected return from current price to middle band (take-profit target)
                decimal expectedReturnPct = currentPrice > 0
                    ? (bb.Middle - currentPrice) / currentPrice
                    : 0m;

                bool signal =
                    expectedReturnPct > _settings.BbMinProfitAboveFees  // covers fees + margin
                    && bb.PercentB < 0m                                  // below lower band
                    && rsi    < _settings.RsiOversold                   // oversold
                    && atrRatio < _settings.AtrVolatilityMultiplier     // not extreme vol
                    && adx    < _settings.AdxThreshold                  // no strong trend
                    && bb.Bandwidth > _settings.BbMinBandwidth;         // bands not squeezed

                if (!signal) continue;

                _logger.Info($"[BB] Signal {symbol}: %B={bb.PercentB:F3} RSI={rsi:F1} ADX={adx:F1} " +
                             $"BW={bb.Bandwidth:P1} ATR×={atrRatio:F2} expectedReturn={expectedReturnPct:P2}");

                if (bestCoin is null || bb.PercentB < bestPctB)
                {
                    bestCoin = new Coin(symbol);
                    bestPctB = bb.PercentB;
                    bestAtr  = atr.Value;
                }
            }

            if (bestCoin is not null)
                await EnterPosition(bestCoin, bestAtr);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private IReadOnlyList<Candle> Candles(string symbol) =>
            _candleRepository.GetCandles($"{symbol}{_settings.Bridge}", WarmupCandles);

        private bool ShouldExit(IReadOnlyList<Candle> candles)
        {
            if (candles.Count < _settings.BbPeriod) return false;

            var bb = BollingerBands(candles, _settings.BbPeriod, _settings.BbStdDev);
            if (bb is null) return false;

            var price = candles[^1].Close;

            if (price >= bb.Middle)
            {
                _logger.Info($"[BB] Exit {_position!.Symbol}: target reached ({price:F8} ≥ mid {bb.Middle:F8})");
                return true;
            }

            // Polling stop-loss fallback (covers gaps when no stop order was placed)
            var stop = _entryPrice - _settings.StopLossAtrMultiplier * _entryAtr;
            if (_stopOrderId == 0 && price <= stop)
            {
                _logger.Info($"[BB] Exit {_position!.Symbol}: stop-loss hit ({price:F8} ≤ {stop:F8})");
                return true;
            }

            return false;
        }

        private async Task<bool> StopOrderFilled()
        {
            try
            {
                var status = await _manager.GetOrder(
                    $"{_position!.Symbol}{_settings.Bridge}", _stopOrderId);

                if (status?.Status != OrderStatus.FILLED) return false;

                _logger.Info($"[BB] Stop-loss order {_stopOrderId} filled by exchange for {_position.Symbol}");
                ClearPosition();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn($"[BB] Could not check stop order {_stopOrderId}: {ex.Message}");
                return false;
            }
        }

        private async Task EnterPosition(Coin coin, decimal atr)
        {
            _logger.Info($"[BB] Entering {coin.Symbol}");

            // Position sizing: use only BbPositionSizePct of available bridge balance
            var bridgeBalance = await _manager.GetCurrencyBalance(_settings.Bridge);
            var positionSize  = bridgeBalance * _settings.BbPositionSizePct;

            var order = await _manager.BuyAlt(coin, _bridge, maxBridgeAmount: positionSize);

            if (order is null)
            {
                _logger.Warn($"[BB] Buy failed for {coin.Symbol} — staying in {_settings.Bridge}");
                return;
            }

            _position   = coin;
            _entryPrice = order.Price;
            _entryAtr   = atr;
            _coinRepository.SaveCurrent(coin);

            // CumulativeFilledQuantity is the actual filled amount from the WS fill event
            _positionQty = order.CumulativeFilledQuantity;

            if (_positionQty <= 0)
                _logger.Warn($"[BB] Filled qty is zero for {coin.Symbol} — stop order skipped");
            else
                await PlaceStopOrder(coin, atr);

            _logger.Info($"[BB] Position open: {coin.Symbol} @ {_entryPrice:F8} qty={_positionQty} " +
                         $"SL={_entryPrice - _settings.StopLossAtrMultiplier * atr:F8}");
        }

        private async Task PlaceStopOrder(Coin coin, decimal atr)
        {
            decimal stopPrice  = _entryPrice - _settings.StopLossAtrMultiplier * atr;
            decimal limitPrice = stopPrice * 0.999m; // 0.1% below trigger to ensure execution

            try
            {
                var stopOrder = await _manager.PlaceStopLoss(
                    $"{coin.Symbol}{_settings.Bridge}", _positionQty, stopPrice, limitPrice);

                _stopOrderId = stopOrder?.OrderId ?? 0;
                _logger.Info($"[BB] Stop order placed @ trigger={stopPrice:F8} limit={limitPrice:F8} id={_stopOrderId}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"[BB] Failed to place stop order: {ex.Message} — polling fallback active");
                _stopOrderId = 0;
            }
        }

        private async Task ExitPosition()
        {
            // Cancel the resting stop order before selling so we don't get a double-sell
            if (_stopOrderId > 0)
            {
                try
                {
                    await _manager.CancelStopOrder($"{_position!.Symbol}{_settings.Bridge}", _stopOrderId);
                    _logger.Info($"[BB] Stop order {_stopOrderId} canceled before selling");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[BB] Could not cancel stop order {_stopOrderId}: {ex.Message}");
                    // Cancel failed — check if the stop already filled to avoid double-selling
                    // a position that the exchange already liquidated for us.
                    if (await StopOrderFilled())
                        return; // ClearPosition was called inside StopOrderFilled
                }
                _stopOrderId = 0;
            }

            _logger.Info($"[BB] Selling {_position!.Symbol} → {_settings.Bridge}");
            var result = await _manager.SellAlt(_position, _bridge);

            if (result is null)
            {
                _logger.Warn($"[BB] Sell failed for {_position.Symbol} — re-placing stop order and retrying next scout");
                // Use current ATR (not stale _entryAtr) so the stop price is relevant to today's volatility
                var freshCandles = Candles(_position.Symbol);
                var freshAtr = Atr(freshCandles) ?? _entryAtr;
                await PlaceStopOrder(_position, freshAtr);
                return;
            }

            ClearPosition();
        }

        private void ClearPosition()
        {
            _coinRepository.SaveCurrent(_bridge);
            _position    = null;
            _entryPrice  = 0;
            _entryAtr    = 0;
            _stopOrderId = 0;
            _positionQty = 0;
        }
    }
}
