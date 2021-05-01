using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

public class LoggerConfig
{
    public int EventId { get; set; }

    public Dictionary<LogLevel, ConsoleColor> LogLevels { get; set; }

    public LoggerConfig()
    {
        LogLevels = new Dictionary<LogLevel, ConsoleColor>
        {
            [LogLevel.Information] = ConsoleColor.Green
        };
    }
}