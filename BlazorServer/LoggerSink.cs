using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Linq;
using System.Collections.Generic;

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

        public static List<LogItem> Log { get; private set; } = new List<LogItem>();

        EventArgs args = new EventArgs();

        public void Emit(LogEvent logEvent)
        {
            Log.Add(new LogItem { Timestamp = logEvent.Timestamp, Level = logEvent.Level, Message = logEvent.RenderMessage() });
            if (Log.Count > 1000)
            {
                Log = Log.Skip(Math.Max(0, Log.Count - 500)).ToList();
            }

            OnLogChanged?.Invoke(this, args);
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
