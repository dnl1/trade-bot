namespace TradeBot
{
    internal interface ILogger
    {
        void Warning(string message);
        void Error(string message);
        void Information(string message);
        void Debug(string message);
    }
}
