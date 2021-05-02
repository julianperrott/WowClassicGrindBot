using Core;
using Core.Addon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PathingAPI;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;
using System.Threading;
using MatBlazor;
using SharedLib;

namespace BlazorServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var logfile = "out.log";
            var config = new LoggerConfiguration()
                //.Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.LoggerSink()
                .WriteTo.File(logfile, rollingInterval: RollingInterval.Day)
                .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss:fff} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            Log.Logger = config.CreateLogger();
            Log.Logger.Debug("Startup()");

            while (WowProcess.Get() == null)
            {
                Log.Information("Unable to find the Wow process is it running ?");
                Thread.Sleep(1000);
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public static void ConfigureServices(IServiceCollection services)
        {
            var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));
            services.AddSingleton(logger);

            var wowProcess = new WowProcess();
            var wowScreen = new WowScreen(logger, wowProcess);
            wowScreen.GetRectangle(out var rect);

            var addonConfig = AddonConfig.Load();
            var addonConfigurator = new AddonConfigurator(logger, addonConfig);

            if(!addonConfig.IsDefault() && !addonConfigurator.Installed())
            {
                // At this point the webpage never loads so fallback to configuration page
                DataFrameConfiguration.RemoveConfiguration();
            }

            if(DataFrameConfiguration.Exists() && 
                !DataFrameConfiguration.IsValid(rect, addonConfigurator.GetInstalledVersion()))
            {
                // At this point the webpage never loads so fallback to configuration page
                DataFrameConfiguration.RemoveConfiguration();
            }

            if (AddonConfig.Exists() && DataFrameConfiguration.Exists())
            {
                var dataConfig = DataConfig.Load();
                var pather = GetPather(logger, dataConfig);
                var botController = new BotController(logger, pather, dataConfig);
                services.AddSingleton<IBotController>(botController);
                services.AddSingleton<IAddonReader>(botController.AddonReader);
                services.AddMatBlazor();
            }
            else
            {
                services.AddSingleton<IBotController>(new ConfigBotController());
                services.AddSingleton<IAddonReader>(new ConfigAddonReader());
            }

            services.AddRazorPages();
            services.AddServerSideBlazor();
        }

        private static IPPather GetPather(Microsoft.Extensions.Logging.ILogger logger, DataConfig dataConfig)
        {
            var api = new RemotePathingAPI(logger);
            if (api.PingServer().Result)
            {
                Log.Information("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                Log.Debug("Using remote pathing API");
                Log.Information("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                return api;
            }

            Log.Information("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Log.Information("Using local pathing API.");
            Log.Information("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            var pathingService = new PPatherService(LogWrite, dataConfig);
            var localApi = new LocalPathingApi(logger, pathingService);
            localApi.SelfTest();
            Log.Information("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            return localApi;
        }

        public static void LogWrite(string message)
        {
            Log.Information(message);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}