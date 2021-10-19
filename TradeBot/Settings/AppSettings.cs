namespace TradeBot.Settings
{
    public class AppSettings
    {
        public string ApiKey { get; set; }
        public string ApiSecretKey { get; set; }
        public string Bridge { get; set; }
        public string Tld { get; set; }
        public string CurrentCoin { get; set; }
        public decimal ScoutMultiplier { get; set; }
        public string Strategy { get; set; }
        public string[] Coins { get; set; }
    }
}
