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
using System.Collections.ObjectModel;
using System.IO;

namespace TradeBot.Repositories
{
    public class BinanceApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ICacher _cacher;
        private readonly ILogger _logger;
        private readonly bool _testnet = false;
        private readonly string _apiKey;
        private readonly string _apiKeySecret;
        private readonly RSA? _rsaPrivateKey;
        private readonly string _tld;

        private readonly string _apiUrl;
        private const string API_TESTNET_URL = "https://testnet.binance.vision/api";
        private readonly string _marginApiUrl;
        private readonly string _websiteUrl;
        private readonly string _futuresUrl;
        private const string FUTURES_TESTNET_URL = "https://testnet.binancefuture.com/fapi";
        private readonly string _futuresDataUrl;
        private const string FUTURES_DATA_TESTNET_URL = "https://testnet.binancefuture.com/futures/data";
        private readonly string _futuresCoinUrl;
        private const string FUTURES_COIN_TESTNET_URL = "https://testnet.binancefuture.com/dapi";
        private readonly string _futuresCoinDataUrl;
        private const string FUTURES_COIN_DATA_TESTNET_URL = "https://testnet.binancefuture.com/futures/data";
        private readonly string _optionsUrl;
        private readonly string _optionsTestnetUrl;

        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Converters = new List<JsonConverter> { new DecimalConverter() }
        };
        private const string PUBLIC_API_VERSION = "v1";
        private const string PRIVATE_API_VERSION = "v3";
        private const string MARGIN_API_VERSION = "v1";

        private const string FUTURES_API_VERSION = "v1";

        private const string FUTURES_API_VERSION2 = "v2";
        private const string OPTIONS_API_VERSION = "v1";

        public BinanceApiClient(HttpClient httpClient, AppSettings settings, ICacher cacher, ILogger logger)
        {
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                throw new KeyNotFoundException("ApiKey not populated");
            }

            var hasSecretKey = !string.IsNullOrEmpty(settings.ApiSecretKey);
            var hasPrivateKeyPath = !string.IsNullOrEmpty(settings.ApiPrivateKeyPath);

            if (!hasSecretKey && !hasPrivateKeyPath)
            {
                throw new KeyNotFoundException("ApiSecretKey or ApiPrivateKeyPath not populated");
            }

            _httpClient = httpClient;
            _cacher = cacher;
            _logger = logger;
            _apiKey = settings.ApiKey;
            _apiKeySecret = settings.ApiSecretKey;
            _rsaPrivateKey = hasPrivateKeyPath ? LoadRsaPrivateKey(settings.ApiPrivateKeyPath) : null;
            _tld = settings.Tld;

            _apiUrl = string.Format("https://api.binance.{0}/api", _tld);
            _marginApiUrl = string.Format("https://api.binance.{0}/sapi", _tld);
            _websiteUrl = string.Format("https://www.binance.{0}", _tld);
            _futuresUrl = string.Format("https://fapi.binance.{0}/fapi", _tld);
            _futuresDataUrl = string.Format("https://fapi.binance.{0}/futures/data", _tld);
            _futuresCoinUrl = string.Format("https://dapi.binance.{0}/dapi", _tld);
            _futuresCoinDataUrl = string.Format("https://dapi.binance.{0}/futures/data", _tld);
            _optionsUrl = string.Format("https://vapi.binance.{0}/vapi", _tld);
            _optionsTestnetUrl = string.Format("https://testnet.binanceops.{0}/vapi", _tld);

            PopHeaders();
        }

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

        internal IReadOnlyDictionary<string, object> CreateSignedWebSocketParameters(Dictionary<string, object>? data = null)
        {
            data ??= new Dictionary<string, object>();
            data["apiKey"] = _apiKey;
            data["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var signaturePayload = ExtractWsSignaturePayload(data);
            data["signature"] = Sign(signaturePayload);

            return new ReadOnlyDictionary<string, object>(data);
        }

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

        internal async Task<Symbol?> GetSymbolInfo(string symbol, int ttl = 43200) =>
            await _cacher.ExecuteAsync(async () =>
            {
                ExchangeInfo? exchangeInfo = null;

                for (int attempt = 0; attempt < 10 && exchangeInfo?.Symbols is null; attempt++)
                {
                    exchangeInfo = await GetExchangeInfo();
                    if (exchangeInfo?.Symbols is null)
                        await Task.Delay(1000);
                }

                if (exchangeInfo?.Symbols is null) return null;

                var symbolObj = exchangeInfo.Symbols.Find(s => s.SymbolName == symbol);

                return symbolObj;
            }, TimeSpan.FromSeconds(ttl), key: symbol);

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

            return await Post<OrderResult>("order", true, data: dict);
        }

        internal async Task<OrderResult> PlaceStopLossOrder(
            string symbol, decimal quantity, decimal stopPrice, decimal limitPrice)
        {
            var dict = new Dictionary<string, string>
            {
                { "symbol",      symbol },
                { "side",        "SELL" },
                { "type",        "STOP_LOSS_LIMIT" },
                { "timeInForce", "GTC" },
                { "quantity",    quantity.ToString(CultureInfo.InvariantCulture) },
                { "price",       limitPrice.ToString(CultureInfo.InvariantCulture) },
                { "stopPrice",   stopPrice.ToString(CultureInfo.InvariantCulture) }
            };
            return await Post<OrderResult>("order", signed: true, data: dict);
        }

        internal async Task<OrderResult> GetOrder(string symbol, long orderId)
        {
            var dict = new Dictionary<string, string>
            {
                { "symbol",  symbol },
                { "orderId", orderId.ToString() }
            };
            return await Get<OrderResult>("order", signed: true,
                version: PRIVATE_API_VERSION, data: dict);
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

        private async Task<T> Get<T>(string path, bool signed = false, string version = PUBLIC_API_VERSION, Dictionary<string, string>? data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            return await Request<T>("GET", url, signed, data);
        }

        private async Task<T> Delete<T>(string path, bool signed = false, string version = PUBLIC_API_VERSION, Dictionary<string, string>? data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            return await Request<T>("DELETE", url, signed, data);
        }

        private async Task<T> Post<T>(string path, bool signed = false, string version = PUBLIC_API_VERSION, Dictionary<string, string>? data = null)
        {
            data ??= new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            return await Request<T>("POST", url, signed, data);
        }

        private async Task<T> RequestMarginApi<T>(string method, string path, bool signed, Dictionary<string, string>? data = null)
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
                data.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
                // Sign BEFORE adding signature to the dict so it is never re-sorted into the middle.
                // Binance requires 'signature' to be the last query-string parameter.
                string signature = Sign(ExtractRawQs(data));
                requestUrl += $"?{ExtractEncodedQs(data)}&signature={Uri.EscapeDataString(signature)}";
            }
            else if (data.Any())
            {
                requestUrl += $"?{ExtractEncodedQs(data)}";
            }

            var response = await _httpClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), requestUrl));

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                T? obj = JsonConvert.DeserializeObject<T>(json, _jsonSettings);
                if (obj is null)
                    throw new JsonSerializationException($"Received null when deserializing {typeof(T)}");

                return obj;
            }
            catch (Exception ex)
            {
                _logger.Error($"Trying to deserialize to {typeof(T)} JSON: {json} EXCEPTION: {ex}");
                throw;
            }
        }

        private static string ExtractRawQs(Dictionary<string, string> data)
        {
            return string.Join("&", data.OrderByDescending(a => a.Key).Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)));
        }

        private static string ExtractEncodedQs(Dictionary<string, string> data)
        {
            return string.Join("&", data
                .OrderByDescending(a => a.Key)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        }

        private static string ExtractWsSignaturePayload(Dictionary<string, object> data)
        {
            return string.Join("&", data
                .Where(kvp => kvp.Key != "signature")
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={Convert.ToString(kvp.Value, CultureInfo.InvariantCulture)}"));
        }

        private string Sign(string payload)
        {
            if (_rsaPrivateKey is not null)
            {
                var signatureBytes = _rsaPrivateKey.SignData(
                    Encoding.UTF8.GetBytes(payload),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                return Convert.ToBase64String(signatureBytes);
            }

            byte[] secretKey = Encoding.UTF8.GetBytes(_apiKeySecret);
            byte[] qsBytes = Encoding.UTF8.GetBytes(payload);
            using HMACSHA256 hmac = new HMACSHA256(secretKey);

            var hashBytes = hmac.ComputeHash(qsBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            _rsaPrivateKey?.Dispose();
        }

        private static RSA LoadRsaPrivateKey(string configuredPath)
        {
            string expandedPath = ExpandHomePath(configuredPath);

            if (!File.Exists(expandedPath))
            {
                throw new FileNotFoundException($"Binance private key file not found at '{expandedPath}'");
            }

            string pem = File.ReadAllText(expandedPath);

            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }

        private static string ExpandHomePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("~"))
                return path;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var relativePath = path.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.Combine(home, relativePath);
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
            string url = _marginApiUrl;
            return url + '/' + version + '/' + path;
        }
        private string CreateApiUri(string path, bool signed, string version = PUBLIC_API_VERSION)
        {
            string url = _apiUrl;

            if (_testnet)
            {
                url = API_TESTNET_URL;

            }
            string v = signed ? PRIVATE_API_VERSION : version;

            return url + '/' + v + '/' + path;
        }

    }
}
