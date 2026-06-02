using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Entities;
using TradeBot.Enums;
using TradeBot.Models;
using TradeBot.Repositories;
using TradeBot.Responses;
using TradeBot.Services;
using TradeBot.Settings;

namespace TradeBot
{
    public class BinanceApiManager
    {
        private readonly AppSettings _settings;
        private readonly ILogger _logger;
        private readonly ISnapshotRepository _snapshotRepository;
        private readonly ITradeService _tradeService;
        private readonly BinanceStreamManager _streamManager;
        private readonly BinanceApiClient _apiClient;

        public BinanceApiManager(AppSettings settings,
            ILogger logger,
            ISnapshotRepository snapshotRepository,
            ITradeService tradeService,
            BinanceStreamManager streamManager,
            BinanceApiClient apiClient)
        {
            _settings = settings;
            _logger = logger;
            _snapshotRepository = snapshotRepository;
            _tradeService = tradeService;
            _streamManager = streamManager;
            _apiClient = apiClient;
        }

        internal async Task<OrderUpdateResult?> BuyAlt(Coin origin, Coin target, decimal? maxBridgeAmount = null)
        {
            _tradeService.StartTradeLog(origin, target, Side.BUY);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;

            var originBalance = await GetCurrencyBalance(originSymbol);
            var targetBalance = await GetCurrencyBalance(targetSymbol);

            // Apply position-size cap if provided
            if (maxBridgeAmount.HasValue)
                targetBalance = Math.Min(targetBalance, maxBridgeAmount.Value);

            decimal fromCoinPrice = await GetTickerPrice(originSymbol + targetSymbol) ?? 0m;
            if (fromCoinPrice <= 0)
            {
                _logger.Warn($"Stopping because price for {originSymbol + targetSymbol} is zero");
                return null;
            }

            double orderQty = await BuyQuantity(originSymbol, targetSymbol, targetBalance, fromCoinPrice);

            if (orderQty == 0)
            {
                _logger.Warn("Stopping because there's no funds to BUY");
                return null;
            }

            _logger.Info($"Buying {orderQty} of {originSymbol}");

            _logger.Info($"Balance is {originBalance}");

            OrderResult? order = null;
            using var orderGuard = _streamManager.AcquireOrderGuard();

            for (int attempt = 0; attempt < 3 && order is null; attempt++)
            {
                try
                {
                    order = await _apiClient.OrderLimitBuy(originSymbol + targetSymbol, orderQty, fromCoinPrice);
                }
                catch (Exception e)
                {
                    _logger.Error($"Error at trying to order limit buy (attempt {attempt + 1}/3): {e}");
                }
            }
            if (order is null || order.OrderId == 0) return null;

            _logger.Info($"BUY ORDER {order}");

            var trade = _tradeService.SetOrdered(originBalance, targetBalance, orderQty);

            orderGuard.SetOrder(order.OrderId);

            var waitedOrder = await WaitForOrder(order.OrderId, orderGuard, originSymbol, targetSymbol);

            if (null == waitedOrder) return null;

            _logger.Info($"Bought {originSymbol}");

            _tradeService.SetComplete(trade, waitedOrder.CumQuoteAssetTransactedQty);

            return waitedOrder;
        }

        internal async Task<OrderResult?> SellAlt(Coin origin, Coin target)
        {
            _tradeService.StartTradeLog(origin, target, Side.SELL);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;

            var originBalance = await GetCurrencyBalance(originSymbol);
            var targetBalance = await GetCurrencyBalance(targetSymbol);

            decimal fromCoinPrice = await GetTickerPrice(originSymbol + targetSymbol) ?? 0m;
            if (fromCoinPrice <= 0)
            {
                _logger.Warn($"Stopping because price for {originSymbol + targetSymbol} is zero");
                return null;
            }

            double orderQty = await SellQuantity(originSymbol, targetSymbol, originBalance);

            if (orderQty == 0)
            {
                _logger.Warn("Stopping because there's no STOCK to SELL");
                return null;
            }

            _logger.Info($"Selling {orderQty} of {originSymbol}");

            _logger.Info($"Balance is {originBalance}");

            OrderResult? order = null;
            using var orderGuard = _streamManager.AcquireOrderGuard();

            for (int attempt = 0; attempt < 3 && order is null; attempt++)
            {
                try
                {
                    order = await _apiClient.OrderLimitSell(originSymbol + targetSymbol, orderQty, fromCoinPrice);
                }
                catch (Exception e)
                {
                    _logger.Error($"Error at trying to order limit sell (attempt {attempt + 1}/3): {e}");
                }
            }
            if (order is null || order.OrderId == 0) return null;

            _logger.Info($"SELL ORDER {order}");

            var trade = _tradeService.SetOrdered(originBalance, targetBalance, orderQty);

            orderGuard.SetOrder(order.OrderId);

            var waitedOrder = await WaitForOrder(order.OrderId, orderGuard, originSymbol, targetSymbol);

            if (null == waitedOrder) return null;

            // Poll until balance drops (max 30 s); skip entirely if we started with zero balance
            if (originBalance > 0)
            {
                decimal newBalance = await GetCurrencyBalance(originSymbol);
                for (int poll = 0; poll < 30 && newBalance >= originBalance; poll++)
                {
                    await Task.Delay(1000);
                    newBalance = await GetCurrencyBalance(originSymbol);
                }
            }

            _logger.Info($"Sold {originSymbol}");

            _tradeService.SetComplete(trade, waitedOrder.CumQuoteAssetTransactedQty);

            return order;
        }

        internal async Task<decimal> GetMinNotional(string symbol, string bridge)
        {
            var symbolObj = await _apiClient.GetSymbolInfo(symbol + bridge);
            if (symbolObj?.Filters is null) return 0m;

            var filter = symbolObj.Filters.Find(a => a.FilterType == "MIN_NOTIONAL");

            return filter?.MinNotional ?? 0m;
        }

        internal async Task<decimal> GetFee(Coin originCoin, Coin targetCoin, bool selling)
        {
            const decimal DefaultTakerFee = 0.001m; // 0.1% fallback
            TradeFee? tradeFee = null;

            for (int attempt = 0; attempt < 10 && tradeFee is null; attempt++)
            {
                var tradeFees = await _apiClient.GetTradeFee();
                tradeFee = tradeFees?.FirstOrDefault(f => f.Symbol.Equals(originCoin.Symbol + _settings.Bridge));
                if (tradeFee is null && attempt < 9)
                    await Task.Delay(1000);
            }

            if (tradeFee is null)
            {
                _logger.Warn($"[GetFee] Symbol {originCoin.Symbol + _settings.Bridge} not found in trade fees — using default {DefaultTakerFee:P1}");
                return DefaultTakerFee;
            }

            var baseFee = tradeFee.TakerCommission;

            bool isUsingBnb = await IsUsingBnbForFees();

            if (!isUsingBnb)
                return baseFee;

            // The discount is only applied if we have enough BNB to cover the fee
            decimal amountTrading = selling ? (decimal)await SellQuantity(originCoin.Symbol, targetCoin.Symbol) : (decimal)await BuyQuantity(originCoin.Symbol, targetCoin.Symbol);

            decimal feeAmountBnb = 0;
            decimal feeAmount = amountTrading * baseFee * 0.75m;
            if (originCoin.Symbol.Equals("BNB"))
                feeAmountBnb = feeAmount;
            else
            {
                var originPrice = await GetTickerPrice(originCoin.Symbol + "BNB");
                if (!originPrice.HasValue || originPrice.Value == 0)
                    return baseFee;

                feeAmountBnb = feeAmount * originPrice.Value;
            }

            var bnbBalance = await GetCurrencyBalance("BNB");

            if (bnbBalance >= feeAmountBnb)
                return baseFee * 0.75m;

            return baseFee;
        }

        private async Task<bool> IsUsingBnbForFees()
        {
            var bnbBurnResult = await _apiClient.GetBnbBurnSpotMargin();
            return bnbBurnResult?.SpotBNBBurn ?? false;
        }

        private async Task<OrderUpdateResult?> WaitForOrder(long orderId, OrderGuard orderGuard, string originSymbol, string targetSymbol)
        {
            var order = await orderGuard.WaitAsync();

            _logger.Debug($"Order created: {order}");

            // Guard against unrecognized terminal states (EXPIRED, REJECTED) or network issues
            for (int iteration = 0; iteration < 120; iteration++)
            {
                if (order.Status == OrderStatus.FILLED)
                {
                    _logger.Debug($"Order filled: {order.OrderId}");
                    return order;
                }

                if (order.Status == OrderStatus.CANCELED)
                {
                    _logger.Info("Order is canceled, going back to scouting mode...");
                    return null;
                }

                if (order.Status == OrderStatus.EXPIRED || order.Status == OrderStatus.REJECTED)
                {
                    _logger.Warn($"Order {orderId} ended with status {order.Status}");
                    return null;
                }

                _logger.Debug($"Waiting for order {orderId} to be filled");

                try
                {
                    if (await ShouldCancelOrder(order))
                    {
                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            var cancelOrder = await _apiClient.CancelOrder(originSymbol + targetSymbol, orderId);
                            if (cancelOrder is not null) break;
                            await Task.Delay(1000);
                        }

                        _logger.Info("Order timeout, canceled");

                        if (order.Status == OrderStatus.PARTIALLY_FILLED && order.Side == "BUY")
                        {
                            _logger.Info("Sell partially filled amount");

                            var orderQty = await SellQuantity(originSymbol, targetSymbol);

                            for (int attempt = 0; attempt < 10; attempt++)
                            {
                                var partiallyOrder = await _apiClient.OrderMarketSell(symbol: originSymbol + targetSymbol, quantity: orderQty);
                                if (partiallyOrder is not null) break;
                                await Task.Delay(1000);
                            }

                            _logger.Info("Going back to scout mode...");
                            return null;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Error at WaitForOrder " + e.ToString());
                }

                await Task.Delay(1000);
                // TryGet returns null if Dispose() removed the entry during the delay
                order = orderGuard.TryGet() ?? order;
            }

            _logger.Warn($"Order {orderId} timed out after 120 polling iterations");
            return null;
        }

        private async Task<bool> ShouldCancelOrder(OrderUpdateResult order)
        {
            // TransactionTime is in milliseconds (Unix ms from Binance WS event `T` field)
            long elapsedMs = DateTimeOffset.Now.ToUnixTimeMilliseconds() - order.TransactionTime;
            long elapsedMinutes = elapsedMs / 60_000;
            int timeout = order.Side == "SELL" ? _settings.SellTimeout : _settings.BuyTimeout;

            if (timeout > 0 && elapsedMinutes > timeout && order.Status == OrderStatus.NEW)
                return true;

            if (timeout > 0 && elapsedMinutes > timeout && order.Status == OrderStatus.PARTIALLY_FILLED)
            {
                if (order.Side == "SELL") return true;

                var currentPrice = await GetTickerPrice(order.Symbol);
                if (currentPrice.HasValue && currentPrice.Value * (1 - 0.001m) > order.Price)
                    return true;
            }

            return false;
        }

        private async Task<double> BuyQuantity(string originSymbol, string targetSymbol, decimal? targetBalance = null, decimal? fromCoinPrice = null)
        {
            targetBalance = targetBalance ?? (await GetCurrencyBalance(targetSymbol));
            fromCoinPrice = fromCoinPrice ?? (await GetTickerPrice(originSymbol + targetSymbol)) ?? 0;

            if (fromCoinPrice <= 0) return 0;

            decimal originTick = await GetAltTick(originSymbol, targetSymbol);

            return Math.Floor((double)targetBalance * Math.Pow(10, (double)originTick) / (double)fromCoinPrice) / Math.Pow(10, (double)originTick);
        }

        private async Task<double> SellQuantity(string originSymbol, string targetSymbol, decimal? originBalance = null)
        {
            originBalance = originBalance ?? (await GetCurrencyBalance(originSymbol));

            decimal originTick = await GetAltTick(originSymbol, targetSymbol);

            return Math.Floor((double)originBalance * Math.Pow(10, (double)originTick)) / Math.Pow(10, (double)originTick);
        }

        private async Task<decimal> GetAltTick(string originSymbol, string targetSymbol)
        {
            var symbol = await _apiClient.GetSymbolInfo(originSymbol + targetSymbol);
            if (symbol?.Filters is null) return 0;

            var filter = symbol.Filters.Find(a => a.FilterType == "LOT_SIZE");
            if (filter?.StepSize is null) return 0;

            return ParseStepSizeDecimals(filter.StepSize);
        }

        /// <summary>
        /// Converts a Binance LOT_SIZE stepSize string to a decimal-place count for Math.Pow(10, n).
        /// More robust than the previous IndexOf('1') approach which failed for stepSizes
        /// like "2.00000000" or "5.00000000" (no '1' character → returned wrong 8 decimal places).
        /// </summary>
        private static decimal ParseStepSizeDecimals(string stepSize)
        {
            if (!decimal.TryParse(stepSize,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var tick) || tick <= 0)
                return 8; // safe default for invalid/suspended symbols

            if (tick >= 1m) return 0; // whole-unit step (1, 2, 5, 10, …)

            // Count decimal places by repeatedly multiplying until >= 1
            int places = 0;
            while (tick < 1m) { tick *= 10m; places++; }
            return places;
        }

        public async Task<decimal> GetCurrencyBalance(string targetSymbol)
        {
            Account? account = null;
            for (int attempt = 0; attempt < 30 && account is null; attempt++)
            {
                account = await GetAccount();
                if (account is null)
                    await Task.Delay(1000);
            }

            return account?.Balances?
                .Find(b => b.Asset.Equals(targetSymbol, StringComparison.Ordinal))
                ?.Free ?? 0m;
        }

        internal async Task<OrderResult> PlaceStopLoss(string symbol, decimal qty, decimal stopPrice, decimal limitPrice)
        {
            stopPrice  = await RoundToTickSize(symbol, stopPrice);
            limitPrice = await RoundToTickSize(symbol, limitPrice);
            return await _apiClient.PlaceStopLossOrder(symbol, qty, stopPrice, limitPrice);
        }

        /// <summary>Rounds a price down to the nearest PRICE_FILTER tickSize for the symbol.</summary>
        internal async Task<decimal> RoundToTickSize(string symbol, decimal price)
        {
            var info   = await _apiClient.GetSymbolInfo(symbol);
            var filter = info?.Filters.Find(f => f.FilterType == "PRICE_FILTER");
            if (filter?.TickSize is null) return price;

            if (!decimal.TryParse(filter.TickSize,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var tick) || tick <= 0)
                return price;

            return Math.Floor(price / tick) * tick;
        }

        internal Task<OrderResult> GetOrder(string symbol, long orderId)
            => _apiClient.GetOrder(symbol, orderId);

        internal Task CancelStopOrder(string symbol, long orderId)
            => _apiClient.CancelOrder(symbol, orderId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal async Task<Account> GetAccount() =>
            await _apiClient.GetAccount();

        public async Task<decimal?> GetTickerPrice(string symbol)
        {
            var response = _snapshotRepository.Get(symbol)?.Price;

            if (response != null) return response;

            var ticker = await _apiClient.GetSymbolTicker(symbol);

            return ticker?.Price;
        }
    }
}
