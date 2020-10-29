using Libs.Goals;
using Libs.GOAP;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Libs
{
    public sealed class BotController : IBotController, IDisposable
    {
        private readonly WowProcess wowProcess;
        private readonly ILogger logger;
        private readonly IPPather pather;

        public AddonReader AddonReader { get; set; }
        public Thread? screenshotThread { get; set; }
        public Thread addonThread { get; set; }
        public Thread? botThread { get; set; }
        //public GoalFactory ActionFactory { get; set; }
        public GoapAgent? GoapAgent { get; set; }
        public RouteInfo? RouteInfo { get; set; }

        private GoalThread? actionThread;

        public WowScreen WowScreen { get; set; }

        private NpcNameFinder npcNameFinder;

        public ClassConfiguration? ClassConfig { get; set; }
        private INodeFinder minimapNodeFinder;

        public IImageProvider? MinimapImageFinder { get; set; }

        private bool Enabled = true;

        public BotController(ILogger logger, IPPather pather)
        {
            wowProcess = new WowProcess(logger);
            wowProcess.KeyPress(ConsoleKey.F3, 400).Wait(); // clear target
            this.WowScreen = new WowScreen(logger);
            this.logger = logger;
            this.pather = pather;

            var frames = DataFrameConfiguration.ConfigurationExists()
                ? DataFrameConfiguration.LoadConfiguration()
                : new List<DataFrame>(); //config.CreateConfiguration(WowScreen.GetAddonBitmap());

            AddonReader = new AddonReader(WowScreen, frames, logger);

            minimapNodeFinder = new MinimapNodeFinder(new PixelClassifier());
            MinimapImageFinder = minimapNodeFinder as IImageProvider;

            addonThread = new Thread(AddonRefreshThread);
            addonThread.Start();

            // wait for addon to read the wow state
            var sw = new Stopwatch();
            sw.Start();
            while (AddonReader.PlayerReader.Sequence == 0 || !Enum.GetValues(typeof(PlayerClassEnum)).Cast<PlayerClassEnum>().Contains(AddonReader.PlayerReader.PlayerClass))
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    logger.LogWarning("There is a problem with the addon, I have been unable to read the player class. Is it running ?");
                    sw.Restart();
                }
                Thread.Sleep(100);
            }

            logger.LogDebug($"Woohoo, I have read the player class. You are a {AddonReader.PlayerReader.PlayerClass}.");

            npcNameFinder = new NpcNameFinder(wowProcess, AddonReader.PlayerReader, logger);
            //ActionFactory = new GoalFactory(AddonReader, logger, wowProcess, npcNameFinder);

            screenshotThread = new Thread(ScreenshotRefreshThread);
            screenshotThread.Start();
        }

        public void AddonRefreshThread()
        {
            while (this.AddonReader.Active && this.Enabled)
            {
                this.AddonReader.AddonRefresh();
                this.GoapAgent?.UpdateWorldState();
            }
            this.logger.LogInformation("Addon thread stoppped!");
        }

        public void ScreenshotRefreshThread()
        {
            var nodeFound = false;
            while (this.Enabled)
            {
                this.WowScreen.DoScreenshot(this.npcNameFinder);

                if (ClassConfig != null && this.ClassConfig.Mode == Mode.AttendedGather)
                {
                    nodeFound = this.minimapNodeFinder.Find(nodeFound) != null;
                }
            }
            this.logger.LogInformation("Screenshot thread stoppped!");
        }

        public bool IsBotActive => actionThread == null ? false : actionThread.Active;

        public void ToggleBotStatus()
        {
            if (actionThread != null)
            {
                if (!actionThread.Active)
                {
                    actionThread.Active = true;
                    botThread = new Thread(() => Task.Factory.StartNew(() => BotThread()));
                    botThread.Start();
                }
                else
                {
                    actionThread.Active = false;
                }
            }
        }

        public async Task BotThread()
        {
            if (this.actionThread != null)
            {
                await wowProcess.KeyPress(ConsoleKey.F3, 400); // clear target

                while (this.actionThread.Active && this.Enabled)
                {
                    await actionThread.GoapPerformGoal();
                }
            }

            await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 500);
            logger.LogInformation("Stopped!");
        }

        public void InitialiseBot()
        {
            ClassConfig = ReadClassConfiguration();

            var blacklist = this.ClassConfig.Mode != Mode.Grind ? new NoBlacklist() : (IBlacklist)new Blacklist(AddonReader.PlayerReader, ClassConfig.NPCMaxLevels_Above, ClassConfig.NPCMaxLevels_Below, ClassConfig.Blacklist, logger);

            //this.currentAction = followRouteAction;


            var actionFactory = new GoalFactory(AddonReader, this.logger, this.wowProcess, npcNameFinder, this.pather);
            var availableActions = actionFactory.CreateGoals(ClassConfig, blacklist);
            RouteInfo = actionFactory.RouteInfo;

            this.GoapAgent = new GoapAgent(AddonReader.PlayerReader, availableActions, blacklist, logger, ClassConfig, this.AddonReader.BagReader);

            this.actionThread = new GoalThread(this.AddonReader.PlayerReader, this.wowProcess, GoapAgent, logger);

            // hookup events between actions
            availableActions.ToList().ForEach(a =>
            {
                a.ActionEvent += this.actionThread.OnActionEvent;
                a.ActionEvent += GoapAgent.OnActionEvent;

                // tell other action about my actions
                availableActions.ToList().ForEach(b =>
                {
                    if (b != a) { a.ActionEvent += b.OnActionEvent; }
                });
            });
        }

        private ClassConfiguration ReadClassConfiguration()
        {
            ClassConfiguration classConfig;
            var requirementFactory = new RequirementFactory(AddonReader.PlayerReader, AddonReader.BagReader, logger);

            var classFilename = $"../json/class/{AddonReader.PlayerReader.PlayerClass.ToString()}.json";
            if (File.Exists(classFilename))
            {
                classConfig = JsonConvert.DeserializeObject<ClassConfiguration>(File.ReadAllText(classFilename));
                classConfig.Initialise(AddonReader.PlayerReader, requirementFactory, logger);
                return classConfig;
            }

            throw new ArgumentOutOfRangeException($"Class config file not found {classFilename}");
        }

        public void Dispose()
        {
            npcNameFinder.Dispose();
        }

        public void StopBot()
        {
            if (actionThread != null)
            {
                actionThread.Active = false;
            }
        }

        public void Shutdown()
        {
            this.Enabled = false;
        }
    }
}