using TradeBot.Logger;

namespace TradeBot
{
    public static class FileSinkExtensions
    {
        public static LoggerBuilder AddFile(this LoggerBuilder builder,
            string logDirectory = "/app/logs",
            LogLevel minLevel = LogLevel.Debug)
        {
            builder.AddSink(new FileSink(logDirectory, minLevel));
            return builder;
        }
    }
}
