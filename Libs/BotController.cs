using System.Threading;
using System.Threading.Tasks;

namespace Libs
{
    public class BotController
    {
        public WowData WowData { get; set; }
        public Thread addonThread;
        public Thread? botThread;
        public Bot WowBot;

        public BotController()
        {
            var colorReader = new WowScreen();

            var config = new DataFrameConfiguration(colorReader);

            var frames = config.ConfigurationExists()
                ? config.LoadConfiguration()
                : config.CreateConfiguration(WowScreen.GetAddonBitmap());

            WowData = new WowData(colorReader, frames);
            addonThread = new Thread(WowData.DoWork);
            addonThread.Start();

            WowBot = new Bot(WowData.PlayerReader);
            
        }

        public void DoWork()
        {
            Task.Factory.StartNew(() => WowBot.DoWork());
        }

        public void ToggleBotStatus()
        {
            if (!WowBot.Active)
            {
                WowBot.Active = true;
                botThread = new Thread(DoWork);
                botThread.Start();
            }
            else
            {
                WowBot.Active = false;
            }
        }
    }
}