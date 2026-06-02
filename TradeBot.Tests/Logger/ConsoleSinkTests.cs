using System.IO;
using FluentAssertions;
using TradeBot.Logger;
using Xunit;

namespace TradeBot.Tests.Logger
{
    public class ConsoleSinkTests
    {
        [Fact]
        public void Emit_should_write_level_and_message_to_console()
        {
            var sink = new ConsoleSink();
            var logEvent = new LogEvent(LogLevel.Info, "test");

            using var writer = new StringWriter();
            var original = System.Console.Out;
            System.Console.SetOut(writer);

            try { sink.Emit(logEvent); }
            finally { System.Console.SetOut(original); }

            var output = writer.ToString();
            output.Should().Contain("INFO");
            output.Should().Contain("test");
        }

        [Fact]
        public void Emit_should_include_utc_timestamp_and_level()
        {
            var sink = new ConsoleSink();
            var logEvent = new LogEvent(LogLevel.Warn, "warning");

            using var writer = new StringWriter();
            var original = System.Console.Out;
            System.Console.SetOut(writer);

            try { sink.Emit(logEvent); }
            finally { System.Console.SetOut(original); }

            var output = writer.ToString();
            output.Should().Contain("UTC");
            output.Should().Contain("WARN");
            output.Should().Contain("warning");
        }

        [Fact]
        public void Emit_should_reset_console_color_after_logging()
        {
            var sink = new ConsoleSink();
            var logEvent = new LogEvent(LogLevel.Error, "error");

            using var writer = new StringWriter();
            var original = System.Console.Out;
            System.Console.SetOut(writer);

            try { sink.Emit(logEvent); }
            finally { System.Console.SetOut(original); }

            // ResetColor() should restore default (Gray in most terminals)
            System.Console.ForegroundColor.Should().NotBe(System.ConsoleColor.Red);
        }
    }
}
