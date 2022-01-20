using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using Cyotek.Collections.Generic;

namespace BlazorServer
{
    public static class LoggerSinkExtensions
    {
        public static LoggerConfiguration LoggerSink(this LoggerSinkConfiguration loggerConfiguration)
        {
            return loggerConfiguration.Sink(new LoggerSink());
        }
    }

    public readonly struct LogItem
    {
        public DateTimeOffset Timestamp { get; init; }
        public LogEventLevel Level { get; init; }
        public string Message { get; init; }
    }

    public class LoggerSink : ILogEventSink
    {
        public static event EventHandler? OnLogChanged;

        public static CircularBuffer<LogItem> Log { get; private set; } = new CircularBuffer<LogItem>(250);

        public void Emit(LogEvent logEvent)
        {
            Log.Put(new LogItem { Timestamp = logEvent.Timestamp, Level = logEvent.Level, Message = logEvent.RenderMessage() });
            OnLogChanged?.Invoke(this, EventArgs.Empty);
        }

        public override bool Equals(object? obj)
        {
            return obj is LoggerSink;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
