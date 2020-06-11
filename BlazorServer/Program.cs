using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BlazorServer
{
    public static class Program
    {
        private static string hostUrl = "http://0.0.0.0:5000";

        public static void Main(string[] args)
        {
            Log.Information("Starting blazor server");

            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>

            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(hostUrl);
                    webBuilder.UseStartup<Startup>();
                })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConsole();
                logging.AddEventSourceLogger();
            });
    }
}