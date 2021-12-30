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

    public class LogItem
    {
        public DateTimeOffset Timestamp { get; set; }
        public LogEventLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LoggerSink : ILogEventSink
    {

        public delegate void OnLogChangedEventHandler(object sender, EventArgs args);

        public static event OnLogChangedEventHandler? OnLogChanged;

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
