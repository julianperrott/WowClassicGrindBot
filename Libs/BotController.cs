using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Libs
{
    public class BotController
    {
        public WowData WowData { get; set; }
        public Thread? screenshotThread;
        public Thread addonThread;
        public Thread? botThread;
        public Bot WowBot;

        public BotController(ILogger logger)
        {
            var colorReader = new WowScreen();

            var config = new DataFrameConfiguration(colorReader);

            var frames = config.ConfigurationExists()
                ? config.LoadConfiguration()
                : config.CreateConfiguration(WowScreen.GetAddonBitmap());

            WowData = new WowData(colorReader, frames, logger);
            addonThread = new Thread(AddonRefreshThread);
            addonThread.Start();

            WowBot = new Bot(WowData, logger);
        }

        public void AddonRefreshThread()
        {
            while (this.WowData.Active)
            {
                this.WowData.AddonRefresh();
            }
        }

        public void ScreenshotRefreshThread()
        {
            while (this.WowBot.Active)
            {
                this.WowBot.DoScreenshot();
            }
        }

        public void ToggleBotStatus()
        {
            if (!WowBot.Active)
            {
                WowBot.Active = true;
                botThread = new Thread(()=> Task.Factory.StartNew(() => WowBot.DoWork()));
                botThread.Start();

                screenshotThread = new Thread(ScreenshotRefreshThread);
                screenshotThread.Start();
            }
            else
            {
                WowBot.Active = false;
            }
        }
    }
}