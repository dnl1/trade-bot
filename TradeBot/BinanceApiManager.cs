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
    internal class BinanceApiManager
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

        internal async Task BuyAlt(Coin origin, Coin target)
        {
            _tradeService.StartTradeLog(origin, target, Side.BUY);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;

            var originBalance = await GetCurrencyBalance(originSymbol);
            var targetBalance = await GetCurrencyBalance(targetSymbol);

            decimal fromCoinPrice = GetTickerPrice(originSymbol + targetSymbol) ?? 0;

            double orderQty = await BuyQuantity(originSymbol, targetSymbol, targetBalance, fromCoinPrice);

            _logger.Info($"BUY QTY {orderQty} of <{originSymbol}>");

            if (orderQty == 0)
            {
                _logger.Warn("Stopping because there's no funds to BUY");
                return;
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

            if (order?.OrderId == 0) return;

            var trade = _tradeService.SetOrdered(originBalance, targetBalance, orderQty);

            if (order.Status != OrderStatus.FILLED)
            {
                orderGuard.SetOrder(order.OrderId);

                WaitForOrder(order.OrderId, orderGuard);
            }


            _logger.Info($"Bought {originSymbol}");

            _tradeService.SetComplete(trade, order.CummulativeQuoteQty);
        }

        internal async Task<decimal> GetFee(Coin fromCoin, Coin targetCoin, bool selling)
        {
            var fees = (await _apiClient.GetTradeFee()).ToList();
            var snapTarget = _snapshotRepository.Get(targetCoin.Symbol);
            throw new NotImplementedException();
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

        private async Task<double> BuyQuantity(string originSymbol, string targetSymbol, decimal? targetBalance, decimal? fromCoinPrice)
        {
            targetBalance = targetBalance ?? (await GetCurrencyBalance(targetSymbol));
            fromCoinPrice = fromCoinPrice ?? (GetTickerPrice(originSymbol + targetSymbol)) ?? 0;

            decimal originTick = await GetAltTick(originSymbol, targetSymbol);

            return Math.Floor((double)targetBalance * Math.Pow(10, (double)originTick) / (double)fromCoinPrice) / Math.Pow(10, (double)originTick);
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

        private async Task<decimal> GetCurrencyBalance(string targetSymbol)
        {
            var account = await GetAccount();

            return account.Balances.Find(b => b.Asset.Equals(targetSymbol, StringComparison.Ordinal)).Free;
        }

        internal async Task<Account> GetAccount()
        {
            return await _apiClient.GetAccount();
        }

        public decimal? GetTickerPrice(string symbol) => _snapshotRepository.Get(symbol)?.Price;

    }
}
