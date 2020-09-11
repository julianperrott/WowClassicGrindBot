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

    public class LoggerSink : ILogEventSink
    {

        public delegate void OnLogChangedEventHandler(object sender, EventArgs args);

        public static event OnLogChangedEventHandler? OnLogChanged;

        public static List<LogEvent> Log { get; private set; } = new List<LogEvent>();

        public void Emit(LogEvent logEvent)
        {
            Log.Add(logEvent);
            if (Log.Count>1000)
            {
                Log = Log.Skip(Math.Max(0, Log.Count - 500)).ToList();
            }

            OnLogChanged?.Invoke(this, new EventArgs());
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
