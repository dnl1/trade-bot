using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Converters;
using TradeBot.Responses;
using TradeBot.Enums;
using TradeBot.Models;
using TradeBot.Settings;
using TradeBot.Database;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TradeBot.Repositories
{
    public class BinanceApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacher _cacher;
        private readonly ILogger _logger;
        private readonly bool _testnet = false;
        private readonly string _apiKey;
        private readonly string _apiKeySecret;
        private readonly string _tld;

        private string API_URL = "https://api.binance.{0}/api";
        private const string API_TESTNET_URL = "https://testnet.binance.vision/api";
        private string MARGIN_API_URL = "https://api.binance.{0}/sapi";
        private string WEBSITE_URL = "https://www.binance.{0}";
        private string FUTURES_URL = "https://fapi.binance.{0}/fapi";
        private const string FUTURES_TESTNET_URL = "https://testnet.binancefuture.com/fapi";
        private string FUTURES_DATA_URL = "https://fapi.binance.{0}/futures/data";
        private const string FUTURES_DATA_TESTNET_URL = "https://testnet.binancefuture.com/futures/data";
        private string FUTURES_COIN_URL = "https://dapi.binance.{0}/dapi";
        private const string FUTURES_COIN_TESTNET_URL = "https://testnet.binancefuture.com/dapi";
        private string FUTURES_COIN_DATA_URL = "https://dapi.binance.{0}/futures/data";
        private const string FUTURES_COIN_DATA_TESTNET_URL = "https://testnet.binancefuture.com/futures/data";
        private string OPTIONS_URL = "https://vapi.binance.{0}/vapi";
        private string OPTIONS_TESTNET_URL = "https://testnet.binanceops.{0}/vapi";
        private const string PUBLIC_API_VERSION = "v1";
        private const string PRIVATE_API_VERSION = "v3";
        private const string MARGIN_API_VERSION = "v1";

        private const string FUTURES_API_VERSION = "v1";

        private const string FUTURES_API_VERSION2 = "v2";
        private const string OPTIONS_API_VERSION = "v1";

        public BinanceApiClient(HttpClient httpClient, AppSettings settings, ICacher cacher, ILogger logger)
        {
            if (string.IsNullOrEmpty(settings.ApiKey) || string.IsNullOrEmpty(settings.ApiSecretKey))
            {
                throw new KeyNotFoundException("ApiKey or ApiSecretKey not populated");
            }

            _httpClient = httpClient;
            _cacher = cacher;
            _logger = logger;
            _apiKey = settings.ApiKey;
            _apiKeySecret = settings.ApiSecretKey;
            _tld = settings.Tld;

            API_URL = string.Format(API_URL, _tld);
            MARGIN_API_URL = string.Format(MARGIN_API_URL, _tld);
            WEBSITE_URL = string.Format(WEBSITE_URL, _tld);
            FUTURES_URL = string.Format(FUTURES_URL, _tld);
            FUTURES_DATA_URL = string.Format(FUTURES_DATA_URL, _tld);
            FUTURES_COIN_URL = string.Format(FUTURES_COIN_URL, _tld);
            FUTURES_COIN_DATA_URL = string.Format(FUTURES_COIN_DATA_URL, _tld);
            OPTIONS_URL = string.Format(OPTIONS_URL, _tld);
            OPTIONS_TESTNET_URL = string.Format(OPTIONS_TESTNET_URL, _tld);

            PopHeaders();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal async Task<ListenKeyWrapper> GetListenKey() =>
            await Post<ListenKeyWrapper>("userDataStream", false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal async Task<BnbBurnResult> GetBnbBurnSpotMargin(int ttl = 60) =>
            await _cacher.ExecuteAsync(async () =>
                await RequestMarginApi<BnbBurnResult>("get", "bnbBurn", true)
            , TimeSpan.FromSeconds(ttl));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal async Task<OrderResult> OrderLimitBuy(string symbol, double orderQty, decimal price) =>
            await OrderLimit(symbol, orderQty, price, Side.BUY);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal async Task<OrderResult> OrderLimitSell(string symbol, double orderQty, decimal price) =>
            await OrderLimit(symbol, orderQty, price, Side.SELL);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<OrderResult> OrderLimit(string symbol, double orderQty, decimal price, Side side) =>
            await Post<OrderResult>("order", true, data: new Dictionary<string, string>
            {
                { "symbol", symbol },
                { "quantity", orderQty.ToString(CultureInfo.InvariantCulture) },
                { "price", price.ToString(CultureInfo.InvariantCulture) },
                { "side", side == Side.BUY ? "BUY" : "SELL" },
                { "type", "LIMIT" },
                { "timeInForce", "GTC" }
            });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<Account> GetAccount()
        {
            return await Get<Account>("account", true, PRIVATE_API_VERSION);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<IEnumerable<TradeFee>> GetTradeFee(int ttl = 43200) =>
            await _cacher.ExecuteAsync(async () =>            
                await RequestMarginApi<IEnumerable<TradeFee>>("get", "asset/tradeFee", true), TimeSpan.FromSeconds(ttl));

        internal async Task<Symbol> GetSymbolInfo(string symbol)
        {
            ExchangeInfo exchangeInfo = null;

            while (null == exchangeInfo && null == exchangeInfo?.Symbols)
            {
                exchangeInfo = await GetExchangeInfo();
            }

            var symbolObj = exchangeInfo.Symbols.Find(s => s.SymbolName == symbol);

            return symbolObj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal async Task<ExchangeInfo> GetExchangeInfo() =>
            await Get<ExchangeInfo>("exchangeInfo", version: PRIVATE_API_VERSION);

        internal async Task<TickerResult> GetSymbolTicker(string symbol)
        {
            var dict = new Dictionary<string, string>()
            {
                { "symbol", symbol }
            };

            return await Get<TickerResult>("ticker/price", version: PRIVATE_API_VERSION, data: dict);
        }

        internal async Task<OrderResult> OrderMarketSell(string symbol, double quantity)
        {
            var dict = new Dictionary<string, string>()
            {
                { "symbol", symbol },
                { "quantity", quantity.ToString(CultureInfo.InvariantCulture) },
                { "side", "SELL" }
            };

            return await OrderMarket(dict);
        }

        private async Task<OrderResult> OrderMarket(Dictionary<string, string> dict)
        {
            dict.Add("type", "MARKET");
            dict.Add("timeInForce", "GTC");

            return await Post<OrderResult>("order", true, data: dict);
        }

        internal async Task<OrderCancelResult> CancelOrder(string symbol, long orderId)
        {
            var dict = new Dictionary<string, string>()
            {
                { "symbol", symbol },
                { "orderId", orderId.ToString() }
            };

            return await Delete<OrderCancelResult>("order", version: PRIVATE_API_VERSION, data: dict);
        }

        private async Task<T> Get<T>(string path, bool signed = false, string version = PUBLIC_API_VERSION, Dictionary<string, string> data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            return await Request<T>("GET", url, signed, data);
        }

        private async Task<T> Delete<T>(string path, bool signed = false, string version = PUBLIC_API_VERSION, Dictionary<string, string> data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            return await Request<T>("DELETE", url, signed, data);
        }

        private async Task<T> Post<T>(string path, bool signed = false, string version = PUBLIC_API_VERSION, Dictionary<string, string> data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            return await Request<T>("POST", url, signed, data);
        }

        private async Task<T> RequestMarginApi<T>(string method, string path, bool signed, Dictionary<string, string> data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateMarginApiUri(path, signed);

            return await Request<T>(method, url, signed, data);
        }

        private async Task<T> Request<T>(string method, string url, bool signed, Dictionary<string, string> data)
        {
            string requestUrl = url;

            if (signed)
            {
                data.Add("timestamp", (DateTimeOffset.Now.ToUnixTimeSeconds() * 1000).ToString());

                string queryString = ExtractQs(data);

                byte[] secretKey = Encoding.UTF8.GetBytes(_apiKeySecret);
                byte[] qsBytes = Encoding.UTF8.GetBytes(queryString);
                using HMACSHA256 hmac = new HMACSHA256(secretKey);

                var hashBytes = hmac.ComputeHash(qsBytes);

                string signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                data.Add("signature", signature);

                queryString = ExtractQs(data);

                requestUrl += $"?{queryString}";
            }
            else if(data.Any())
            {
                string queryString = ExtractQs(data);
                requestUrl += $"?{queryString}";
            }

            var response = await _httpClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), requestUrl));

            var json = await response.Content.ReadAsStringAsync();

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new DecimalConverter() }
            };

            try
            {
                T obj = JsonConvert.DeserializeObject<T>(json, settings);
                return obj;
            }
            catch (Exception ex)
            {
                _logger.Error($"Trying to deserialize to {typeof(T)} JSON: {json} EXCEPTION: {ex}");
                throw;
            }
        }

        private static string ExtractQs(Dictionary<string, string> data)
        {
            return string.Join("&", data.OrderByDescending(a => a.Key).Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)));
        }

        private void PopHeaders()
        {

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            }
        }

        private string CreateMarginApiUri(string path, bool signed, string version = MARGIN_API_VERSION)
        {
            string url = MARGIN_API_URL;
            return url + '/' + version + '/' + path;
        }
        private string CreateApiUri(string path, bool signed, string version = PUBLIC_API_VERSION)
        {
            string url = API_URL;

            if (_testnet)
            {
                url = API_TESTNET_URL;

            }
            string v = signed ? PRIVATE_API_VERSION : version;

            return url + '/' + v + '/' + path;
        }

    }
}
