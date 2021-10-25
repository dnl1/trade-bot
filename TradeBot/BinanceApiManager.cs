using System;
using System.Linq;
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
        private readonly string _baseUrl;
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

        internal async Task<OrderUpdateResult> BuyAlt(Coin origin, Coin target)
        {
            _tradeService.StartTradeLog(origin, target, Side.BUY);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;

            var originBalance = await GetCurrencyBalance(originSymbol);
            var targetBalance = await GetCurrencyBalance(targetSymbol);

            decimal fromCoinPrice = await GetTickerPrice(originSymbol + targetSymbol) ?? 0m;

            double orderQty = await BuyQuantity(originSymbol, targetSymbol, targetBalance, fromCoinPrice);

            _logger.Info($"BUY QTY {orderQty} of <{originSymbol}>");

            if (orderQty == 0)
            {
                _logger.Warn("Stopping because there's no funds to BUY");
                return null;
            }

            OrderResult? order = null;
            var orderGuard = _streamManager.AcquireOrderGuard();

            while (null == order)
            {
                try
                {
                    order = await _apiClient.OrderLimitBuy(originSymbol + targetSymbol, orderQty, fromCoinPrice);
                }
                catch (Exception e)
                {
                    _logger.Error($"Error at trying to order limit buy {e}");
                }
            }

            if (order?.OrderId == 0) return null;

            var trade = _tradeService.SetOrdered(originBalance, targetBalance, orderQty);

            orderGuard.SetOrder(order.OrderId);

            var waitedOrder = await WaitForOrder(order.OrderId, orderGuard, originSymbol, targetSymbol);

            if (null == waitedOrder) return null;

            _logger.Info($"Bought {originSymbol}");

            _tradeService.SetComplete(trade, waitedOrder.CumQuoteAssetTransactedQty);

            return waitedOrder;
        }

        internal async Task<OrderResult> SellAlt(Coin origin, Coin target)
        {
            _tradeService.StartTradeLog(origin, target, Side.SELL);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;

            var originBalance = await GetCurrencyBalance(originSymbol);
            var targetBalance = await GetCurrencyBalance(targetSymbol);

            decimal fromCoinPrice = await GetTickerPrice(originSymbol + targetSymbol) ?? 0m;

            double orderQty = await SellQuantity(originSymbol, targetSymbol, originBalance);

            if (orderQty == 0)
            {
                _logger.Warn("Stopping because there's no STOCK to SELL");
                return null;
            }

            _logger.Info($"Selling {orderQty} of {originSymbol}");

            _logger.Info($"Balance is {originBalance}");

            OrderResult order = null;
            var orderGuard = _streamManager.AcquireOrderGuard();

            while (null == order)
            {
                try
                {
                    order = await _apiClient.OrderLimitSell(originSymbol + targetSymbol, orderQty, fromCoinPrice);
                }
                catch (Exception e)
                {
                    _logger.Error($"Error at trying to order limit sell {e}");
                    return null;
                }
            }

            if (order?.OrderId == 0) return null;

            _logger.Info($"SELL ORDER {order}");

            var trade = _tradeService.SetOrdered(originBalance, targetBalance, orderQty);

            orderGuard.SetOrder(order.OrderId);

            var waitedOrder = await WaitForOrder(order.OrderId, orderGuard, originSymbol, targetSymbol);

            if (null == waitedOrder) return null;

            decimal newBalance = await GetCurrencyBalance(originSymbol);
            while (newBalance >= originBalance)
            {
                newBalance = await GetCurrencyBalance(originSymbol);
                Thread.Sleep(1000);
            }

            _logger.Info($"Sold {originSymbol}");

            _tradeService.SetComplete(trade, order.CummulativeQuoteQty);

            return order;
        }

        internal async Task<decimal> GetMinNotional(string symbol, string bridge)
        {
            var symbolObj = await _apiClient.GetSymbolInfo(symbol + bridge);

            var filter = symbolObj.Filters.Find(a => a.FilterType == "MIN_NOTIONAL");

            return filter.MinNotional;
        }

        internal async Task<decimal> GetFee(Coin originCoin, Coin targetCoin, bool selling)
        {
            var baseFee = (await _apiClient.GetTradeFee()).FirstOrDefault(f => f.Symbol.Equals(originCoin.Symbol + _settings.Bridge)).TakerCommission;

            bool isUsingBnb = await IsUsingBnbForFees();

            if (!isUsingBnb)
                return baseFee;

            // The discount is only applied if we have enough BNB to cover the fee
            decimal amountTrading = selling ? (decimal)await SellQuantity(originCoin.Symbol, targetCoin.Symbol) : (decimal)await BuyQuantity(originCoin.Symbol, targetCoin.Symbol);

            decimal feeAmountBnb = 0;
            decimal feeAmount = amountTrading * baseFee * 0.75m;
            if (originCoin.Symbol.Equals("BNB"))
                feeAmountBnb = (decimal)feeAmount;
            else
            {
                var originPrice = await GetTickerPrice(originCoin.Symbol + "BNB");
                if (originPrice.Value == 0)
                    return baseFee;

                feeAmountBnb = (decimal)feeAmount * originPrice.Value;
            }

            var bnbBalance = await GetCurrencyBalance("BNB");

            if (bnbBalance >= feeAmountBnb)
                return baseFee * 0.75m;

            return baseFee;
        }

        private async Task<bool> IsUsingBnbForFees()
        {
            var bnbBurnResult = await _apiClient.GetBnbBurnSpotMargin();

            return bnbBurnResult.SpotBNBBurn;
        }

        private async Task<OrderUpdateResult> WaitForOrder(long orderId, OrderGuard orderGuard, string originSymbol, string targetSymbol)
        {
            OrderUpdateResult? order = null;

            while (null == order)
            {
                if (BinanceCache.Orders.ContainsKey(orderId))
                {
                    order = BinanceCache.Orders[orderId];
                    _logger.Debug($"Waiting for order {orderId} to be created");
                }
                else
                {
                    orderGuard.Wait(orderId);
                }

                Thread.Sleep(1000);
            }

            _logger.Debug($"Order created: {order}");

            while (order.Status != OrderStatus.FILLED)
            {
                try
                {
                    order = BinanceCache.Orders[orderId];

                    _logger.Debug($"Waiting for order {orderId} to be filled");

                    if (await ShouldCancelOrder(order))
                    {
                        OrderCancelResult? cancelOrder = null;
                        while (cancelOrder == null)
                        {
                            cancelOrder = await _apiClient.CancelOrder(originSymbol + targetSymbol, orderId);
                        }

                        _logger.Info("Order timeout, canceled");

                        if (order.Status == OrderStatus.PARTIALLY_FILLED && order.Side == "BUY")
                        {
                            _logger.Info("Sell partially filled amount");

                            var orderQty = await SellQuantity(originSymbol, targetSymbol);

                            OrderResult partiallyOrder = null;

                            while (partiallyOrder == null)
                            {
                                partiallyOrder = await _apiClient.OrderMarketSell(symbol: originSymbol + targetSymbol, quantity: orderQty);
                            }

                            _logger.Info("Going back to scout mode...");

                            return null;
                        }
                    }

                    if (order.Status == OrderStatus.CANCELED)
                    {
                        _logger.Info("Order is canceled, going back to scouting mode...");
                        return null;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Error at WaitForOrder " + e.ToString());
                }

                if (order.Status != OrderStatus.FILLED)
                    Thread.Sleep(1000);
            }

            _logger.Debug($"Order filled: {order.OrderId}");

            return order;
        }

        private async Task<bool> ShouldCancelOrder(OrderUpdateResult order)
        {
            long minutes = (DateTimeOffset.Now.ToUnixTimeSeconds() - order.TransactionTime) / 60;
            int timeout = order.Side == "SELL" ? _settings.SellTimeout : _settings.BuyTimeout;

            if (timeout > 0 && minutes > timeout && order.Status == OrderStatus.NEW)
                return true;

            if (timeout > 0 && minutes > timeout && order.Status == OrderStatus.PARTIALLY_FILLED)
            {
                if (order.Side == "SELL") return true;

                var currentPrice = (await GetTickerPrice(order.Symbol)).Value;

                if (currentPrice * (1 - 0.001m) > order.Price)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<double> BuyQuantity(string originSymbol, string targetSymbol, decimal? targetBalance = null, decimal? fromCoinPrice = null)
        {
            targetBalance = targetBalance ?? (await GetCurrencyBalance(targetSymbol));
            fromCoinPrice = fromCoinPrice ?? (await GetTickerPrice(originSymbol + targetSymbol)) ?? 0;

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
            var filter = symbol.Filters.Find(a => a.FilterType == "LOT_SIZE");
            var stepSize = filter.StepSize;

            if (stepSize.IndexOf('1') == 0)
                return 1 - stepSize.IndexOf('.');

            return stepSize.IndexOf('1') - 1;
        }

        public async Task<decimal> GetCurrencyBalance(string targetSymbol)
        {
            Account account = null;
            decimal? free = null;

            while(null == account && null == free)
            {
                account = await GetAccount();

                if(null != account)
                {
                    free = account?.Balances?.Find(b => b.Asset.Equals(targetSymbol, StringComparison.Ordinal))?.Free;
                }
            }

            return free.GetValueOrDefault();
        }

        internal async Task<Account> GetAccount()
        {
            return await _apiClient.GetAccount();
        }

        public async Task<decimal?> GetTickerPrice(string symbol)
        {
            //var response = _snapshotRepository.Get(symbol)?.Price;

            //if (response != null) return response;

            var ticker = await _apiClient.GetSymbolTicker(symbol);

            return ticker?.Price;
        }
    }
}
