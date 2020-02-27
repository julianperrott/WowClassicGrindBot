using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BlazorServer.Data;
using Libs;
using System.Threading;
using Libs.Looting;

namespace BlazorServer
{
    public class Startup
    {
        public static AddonThread AddonThread { get; set; }
        public static Thread addonThread;
        public static Thread botThread;
        public static Bot WowBot;
        public static bool active=false;

        static Startup()
        {
            var colorReader = new WowScreen();

            var config = new DataFrameConfiguration(colorReader);

            var frames = config.ConfigurationExists()
                ? config.LoadConfiguration()
                : config.CreateConfiguration(WowScreen.GetAddonBitmap());

            AddonThread = new AddonThread(colorReader, frames);
            addonThread = new Thread(AddonThread.DoWork);

            WowBot = new Bot(AddonThread.PlayerReader);
            botThread = new Thread(DoWork);
        }

        public static void DoWork()
        {
            Task.Factory.StartNew(() => WowBot.DoWork());
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            addonThread.Start();
        }

        public static void ToggleBotStatus()
        {
            if (!active)
            {
                botThread.Start();
            }
            else
            {
                botThread.Abort();
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
