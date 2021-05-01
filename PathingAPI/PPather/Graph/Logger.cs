using Microsoft.Extensions.Logging;
using System;

namespace PatherPath
{
    public class Logger : ILogger
    {
        private readonly string _name = "Logger";
        private readonly LoggerConfig _config;

        public Logger()
        {
            //_config = loggerConfig;
            _config = new LoggerConfig();
        }

        private Action<string> onWrite;

        public Logger(Action<string> action)
        {
            this.onWrite = action;
            _config = new LoggerConfig();
        }

        public void WriteLine(string message)
        {
            if (onWrite != null)
            {
                onWrite(message);
            }
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Debug(string message)
        {
            WriteLine(message);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            //if (_config.EventId == 0 || _config.EventId == eventId.Id)
            //{
                //ConsoleColor originalColor = Console.ForegroundColor;

                //Console.ForegroundColor = _config.LogLevels[logLevel];
                //Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

                //Console.ForegroundColor = originalColor;
                //Console.WriteLine($"     {_name} - {formatter(state, exception)}");
                System.Diagnostics.Debug.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");
                System.Diagnostics.Debug.WriteLine($"     {_name} - {formatter(state, exception)}");
            //}
        }

        public bool IsEnabled(LogLevel logLevel) =>
            _config.LogLevels.ContainsKey(logLevel);

        public IDisposable BeginScope<TState>(TState state) => default;
    }
}