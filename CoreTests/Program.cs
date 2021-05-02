using Serilog;
using Serilog.Extensions.Logging;

namespace CoreTests
{
    class Program
    {
        private static Microsoft.Extensions.Logging.ILogger logger;

        private static void CreateLogger()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.File("names.log")
                .CreateLogger();

            Log.Logger = logConfig;
            logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));
        }

        static void Main(string[] args)
        {
            CreateLogger();

            var test = new Test_NpcNameFinder(logger);
            test.Execute();
        }
    }
}
