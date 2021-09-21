namespace TradeBot.Settings
{
    internal class AppSettings
    {
        public string ApiKey { get; set; }
        public string ApiSecretKey { get; set; }
        public string Bridge { get; set; }
        public string Tld { get; set; }
        public string CurrentCoin { get; set; }
        public string Strategy { get; set; }
        public string[] Coins { get; set; }
    }
}
