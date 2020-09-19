using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

namespace BlazorServer
{
    public static class Program
    {
        private static string hostUrl = "http://0.0.0.0:5000";

        public static void Main(string[] args)
        {
            while (true)
            {
                Log.Information("Program.Main(): Starting blazor server");
                try
                {
                    CreateHostBuilder(args)
                        .Build()
                        .Run();
                }
                catch(Exception ex)
                {
                    Log.Information($"Program.Main(): {ex.Message}");
                    Log.Information("");
                }
            }
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