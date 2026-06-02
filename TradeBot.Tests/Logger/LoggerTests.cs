using System;
using System.Collections.Generic;
using FluentAssertions;
using TradeBot.Logger;
using Xunit;

namespace TradeBot.Tests.Logger
{
    public class LoggerTests
    {
        [Fact]
        public void Debug_should_emit_log_event_with_correct_level()
        {
            var sink = new FakeSink();
            var logger = new TradeBot.Logger.Logger(new[] { sink });

            logger.Debug("test message");

            sink.Events.Should().ContainSingle();
            sink.Events[0].Level.Should().Be(LogLevel.Debug);
            sink.Events[0].Message.Should().Be("test message");
        }

        [Fact]
        public void Info_should_emit_log_event_with_correct_level()
        {
            var sink = new FakeSink();
            var logger = new TradeBot.Logger.Logger(new[] { sink });

            logger.Info("info message");

            sink.Events.Should().ContainSingle();
            sink.Events[0].Level.Should().Be(LogLevel.Info);
        }

        [Fact]
        public void Warn_should_emit_log_event_with_correct_level()
        {
            var sink = new FakeSink();
            var logger = new TradeBot.Logger.Logger(new[] { sink });

            logger.Warn("warn message");

            sink.Events.Should().ContainSingle();
            sink.Events[0].Level.Should().Be(LogLevel.Warn);
        }

        [Fact]
        public void Error_should_emit_log_event_with_correct_level()
        {
            var sink = new FakeSink();
            var logger = new TradeBot.Logger.Logger(new[] { sink });

            logger.Error("error message");

            sink.Events.Should().ContainSingle();
            sink.Events[0].Level.Should().Be(LogLevel.Error);
        }

        [Fact]
        public void Emit_should_send_to_all_sinks()
        {
            var sink1 = new FakeSink();
            var sink2 = new FakeSink();
            var logger = new TradeBot.Logger.Logger(new[] { sink1, sink2 });

            logger.Info("broadcast");

            sink1.Events.Should().ContainSingle();
            sink2.Events.Should().ContainSingle();
        }

        [Fact]
        public void Constructor_should_throw_when_no_sinks()
        {
            Action act = () => new TradeBot.Logger.Logger(Array.Empty<ILogEventSink>());

            act.Should().Throw<InvalidOperationException>();
        }

        private class FakeSink : ILogEventSink
        {
            public List<LogEvent> Events { get; } = new();

            public void Emit(LogEvent logEvent) => Events.Add(logEvent);
        }
    }
}
