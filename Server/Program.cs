using Serilog;
using Serilog.Events;
using SharedLib;
using Serilog.Extensions.Logging;
using System.Threading;
using CommandLine;

namespace Server
{
    public class Program
    {
        public class Options
        {
            [Option('p', "port", Required = false, HelpText = "Listening Port default=9050")]
            public int Port { get; set; }
        }

        private static Microsoft.Extensions.Logging.ILogger logger;

        private static void CreateLogger()
        {
            var logfile = "server_out.log";
            var config = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(logfile, rollingInterval: RollingInterval.Day);

            Log.Logger = config.CreateLogger();
            logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));
        }

        private static void WaitForProcess()
        {
            while (WowProcess.Get() == null)
            {
                Log.Information("Unable to find the Wow process is it running ?");
                Thread.Sleep(1000);
            }
        }

        private static void CreateConfig(WowScreen wowScreen)
        {
            /*
            var dataFrameMeta = DataFrameMeta.Empty;
            while(dataFrameMeta == DataFrameMeta.Empty)
            {
                var screenshot = wowScreen.GetBitmap(5, 5);
                var tempFrameMeta = DataFrameConfiguration.GetMeta(screenshot);
                Thread.Sleep(1000);
            }

            var dataFrames = DataFrameConfiguration.CreateFrames(dataFrameMeta, screenshot);

            while (dataFrames.Count != dataFrameMeta.frames)
            {
                dataFrames = DataFrameConfiguration.CreateFrames(dataFrameMeta, screenshot);

                Thread.Sleep(1000);
            }
            */
        }

        static void Main(string[] args)
        {
            int port = 9050;

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                if(o.Port != 0)
                    port = o.Port;
            });

            CreateLogger();
            WaitForProcess();

            var wowProcess = new WowProcess();
            var wowScreen = new WowScreen(logger, wowProcess);

            if (!DataFrameConfiguration.Exists())
            {
                CreateConfig(wowScreen);
            }
            else
            {
                var frames = DataFrameConfiguration.LoadFrames();
                IDataProvider provider = new DataProvider(logger, wowScreen, frames);
                Network.Server server = new Network.Server(logger, port, provider);
                server.ListenServer();
            }

        }
    }
}
