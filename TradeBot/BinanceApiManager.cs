using System;
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

        internal async Task<OrderResult> BuyAlt(Coin origin, Coin target)
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

            OrderResult order = null;
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

            if (order?.Status != OrderStatus.FILLED)
            {
                orderGuard.SetOrder(order.OrderId);

                WaitForOrder(order.OrderId, orderGuard);
            }

            _logger.Info($"Bought {originSymbol}");

            _tradeService.SetComplete(trade, order.CummulativeQuoteQty);

            return order;
        }

        internal async Task SellAlt(Coin origin, Coin target)
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
                return;
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
                    _logger.Error($"Error at trying to order limit buy {e}");
                }
            }

            if (order?.OrderId == 0) return;

            _logger.Info($"SELL ORDER {order}");

            var trade = _tradeService.SetOrdered(originBalance, targetBalance, orderQty);

            if (order?.Status != OrderStatus.FILLED)
            {
                orderGuard.SetOrder(order.OrderId);

                WaitForOrder(order.OrderId, orderGuard);
            }

            decimal newBalance = await GetCurrencyBalance(originSymbol);
            while (newBalance >= originBalance)
            {
                newBalance = await GetCurrencyBalance(originSymbol);
                Thread.Sleep(1000);
            }

            _logger.Info($"Sold {originSymbol}");

            _tradeService.SetComplete(trade, order.CummulativeQuoteQty);
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
            decimal amountTrading = selling ? (decimal)await BuyQuantity(originCoin.Symbol, targetCoin.Symbol) : (decimal)await SellQuantity(originCoin.Symbol, targetCoin.Symbol);

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

        private void WaitForOrder(long orderId, OrderGuard orderGuard)
        {
            OrderUpdateResult? order = null;

            while (null == order)
            {
                if (BinanceCache.Orders.ContainsKey(orderId))
                {
                    order = BinanceCache.Orders[orderId];
                    _logger.Debug($"Waiting for order {orderId} to be created");
                }

                Thread.Sleep(1000);
            }

            _logger.Debug($"Order created: {order}");

            var mutex = orderGuard.GetMutex(orderId);

            while (order.Status != "FILLED")
            {
                order = BinanceCache.Orders[orderId];

                _logger.Debug($"Waiting for order {orderId} to be filled");

                //long orderId = obj?.OrderId ?? 0;
                //if (_mutexes.ContainsKey(orderId))
                //{
                //    _mutexes[orderId].ReleaseMutex();
                //}
            }

            //_logger.debug($"Order filled: {order_status}")


            mutex.WaitOne();
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

            return Math.Floor((double)originBalance * Math.Pow(10, (double)originTick) / Math.Pow(10, (double)originTick));
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
            var account = await GetAccount();

            return account.Balances.Find(b => b.Asset.Equals(targetSymbol, StringComparison.Ordinal)).Free;
        }

        internal async Task<Account> GetAccount()
        {
            return await _apiClient.GetAccount();
        }

        public async Task<decimal?> GetTickerPrice(string symbol)
        {
            var response = _snapshotRepository.Get(symbol)?.Price;

            if (response != null) return response;

            var ticker = await _apiClient.GetSymbolTicker(symbol);

            return ticker?.Price;
        }
    }
}
