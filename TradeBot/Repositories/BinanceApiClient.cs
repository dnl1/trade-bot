using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TradeBot.Settings;

namespace TradeBot.Repositories
{
    internal class BinanceApiClient
    {
        private readonly HttpClient _httpClient;
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

        public BinanceApiClient(HttpClient httpClient, AppSettings settings)
        {
            if(settings.ApiKey == null || settings.ApiSecretKey == null)
            {
                throw new KeyNotFoundException("ApiKey or ApiSecretKey not populated");
            }

            _httpClient = httpClient;
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

        public async Task GetAccount()
        {
            await Get("account", true, PRIVATE_API_VERSION);
        }

        private async Task Get(string path, bool signed, string version, Dictionary<string, string> data = null)
        {
            data = data ?? new Dictionary<string, string>();

            string url = CreateApiUri(path, signed, version);

            await Request("GET", url, signed, data);
        }

        private async Task Request(string method, string url, bool signed, Dictionary<string, string> data)
        {
            data.Add("timestamp", (DateTimeOffset.Now.ToUnixTimeSeconds() * 1000).ToString());

            string queryString = ExtractQs(data);

            if (signed)
            {

                byte[] secretKey = Encoding.UTF8.GetBytes(_apiKeySecret);
                byte[] qsBytes = Encoding.UTF8.GetBytes(queryString);
                using HMACSHA256 hmac = new HMACSHA256(secretKey);

                var hashBytes = hmac.ComputeHash(qsBytes);

                string signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();   

                data.Add("signature", signature);                
            }

            queryString = ExtractQs(data);

            string requestUrl = $"{url}?{queryString}";

            var response = await _httpClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), requestUrl));

            var str = await response.Content.ReadAsStringAsync();
        }

        private static string ExtractQs(Dictionary<string, string> data)
        {
            return string.Join("&", data.OrderByDescending(a => a.Key).Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)));
        }

        private void PopHeaders()
        {

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            /*_httpClient.DefaultRequestHeaders
                .UserAgent
                .Add(new ProductInfoHeaderValue("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36"));*/

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
            }
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
