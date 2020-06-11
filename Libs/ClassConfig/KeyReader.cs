using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public static class KeyReader
    {
        public static Dictionary<string, ConsoleKey> KeyMapping { get; } = new Dictionary<string, ConsoleKey>()
        {
            {"0",ConsoleKey.D0 },
            {"1",ConsoleKey.D1 },
            {"2",ConsoleKey.D2 },
            {"3",ConsoleKey.D3 },
            {"4",ConsoleKey.D4 },
            {"5",ConsoleKey.D5 },
            {"6",ConsoleKey.D6 },
            {"7",ConsoleKey.D7 },
            {"8",ConsoleKey.D8 },
            {"9",ConsoleKey.D9 },
            {"-",ConsoleKey.OemMinus },
            {"=",ConsoleKey.OemPlus },
        };

        public static bool ReadKey(ILogger logger, KeyConfiguration key)
        {
            if (string.IsNullOrEmpty(key.Key))
            {
                logger.LogError($"You must specify either 'Key' (ConsoleKey value) or 'KeyName' (ConsoleKey enum name) for { key.Name}");
                return false;
            }

            if (KeyMapping.ContainsKey(key.Key))
            {
                key.ConsoleKey = KeyMapping[key.Key];
            }
            else
            {
                var values = Enum.GetValues(typeof(ConsoleKey)) as IEnumerable<ConsoleKey>;
                if (values == null) { return false; }
                var consoleKey = values.FirstOrDefault(k => k.ToString() == key.Key);

                if (consoleKey == 0)
                {
                    logger.LogError($"You must specify a valid 'KeyName' (ConsoleKey enum name) for { key.Name}");
                    return false;
                }

                key.ConsoleKey = consoleKey;
            }

            return true;
        }
    }
}