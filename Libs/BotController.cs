using Libs.Actions;
using Libs.GOAP;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public AddonReader AddonReader { get; set; }
        public Thread? screenshotThread { get; set; }
        public Thread addonThread { get; set; }
        public Thread? botThread { get; set; }
        public ActionFactory ActionFactory { get; set; }
        public GoapAgent? GoapAgent { get; set; }
        public RouteInfo? RouteInfo { get; set; }

        private ActionThread? actionThread;

        public WowScreen WowScreen { get; set; }

        private NpcNameFinder npcNameFinder;

        public ClassConfiguration? ClassConfig { get; set; }
        private INodeFinder minimapNodeFinder;

        public IImageProvider? MinimapImageFinder { get; set; }

        public BotController(ILogger logger)
        {
            wowProcess = new WowProcess(logger);
            this.WowScreen = new WowScreen(logger);
            this.logger = logger;

            var frames = DataFrameConfiguration.ConfigurationExists()
                ? DataFrameConfiguration.LoadConfiguration()
                : new List<DataFrame>(); //config.CreateConfiguration(WowScreen.GetAddonBitmap());

            AddonReader = new AddonReader(WowScreen, frames, logger);

            minimapNodeFinder = new MinimapNodeFinder(new PixelClassifier());
            MinimapImageFinder = minimapNodeFinder as IImageProvider;

            addonThread = new Thread(AddonRefreshThread);
            addonThread.Start();

            // wait for addon to read the wow state
            while (AddonReader.PlayerReader.Sequence == 0 || !Enum.GetValues(typeof(PlayerClassEnum)).Cast<PlayerClassEnum>().Contains(AddonReader.PlayerReader.PlayerClass))
            {
                logger.LogWarning("There is a problem with the addon, I have been unable to read the player class. Is it running ?");
                Thread.Sleep(100);
            }

            npcNameFinder = new NpcNameFinder(wowProcess, AddonReader.PlayerReader, logger);
            ActionFactory = new ActionFactory(AddonReader, logger, wowProcess, npcNameFinder);

            screenshotThread = new Thread(ScreenshotRefreshThread);
            screenshotThread.Start();
        }

        public void AddonRefreshThread()
        {
            while (this.AddonReader.Active)
            {
                this.AddonReader.AddonRefresh();
                this.GoapAgent?.UpdateWorldState();
            }
        }

        public void ScreenshotRefreshThread()
        {
            var nodeFound = false;
            while (true)
            {
                this.WowScreen.DoScreenshot(this.npcNameFinder);

                if (ClassConfig != null && this.ClassConfig.Mode == Mode.AttendedGather)
                {
                    nodeFound = this.minimapNodeFinder.Find(nodeFound) != null;
                }
            }
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

                while (this.actionThread.Active)
                {
                    await actionThread.GoapPerformAction();
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

            var actionFactory = new ActionFactory(AddonReader, this.logger, this.wowProcess, npcNameFinder);
            var availableActions = actionFactory.CreateActions(ClassConfig, blacklist);
            RouteInfo = actionFactory.RouteInfo;

            this.GoapAgent = new GoapAgent(AddonReader.PlayerReader, availableActions, blacklist, logger, ClassConfig);

            this.actionThread = new ActionThread(this.AddonReader.PlayerReader, this.wowProcess, GoapAgent, logger);

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
    }
}