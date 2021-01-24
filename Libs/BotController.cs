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
using Libs.Utils;

namespace Libs
{
    public sealed class BotController : IBotController, IDisposable
    {
        private readonly WowProcess wowProcess;
        private readonly ILogger logger;
        private readonly IPPather pather;

        public string? SelectedClassProfile { get; set; }
        public string? SelectedPath { get; set; }

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

        public event EventHandler? ProfileLoaded;

        public BotController(ILogger logger, IPPather pather)
        {
            updatePlayerPostion.Start();
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

        Stopwatch updatePlayerPostion = new Stopwatch();

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

                if (updatePlayerPostion.ElapsedMilliseconds > 500)
                {
                    this.pather.DrawSphere(new Libs.PPather.SphereArgs
                    {
                        Colour = AddonReader.PlayerReader.PlayerBitValues.PlayerInCombat ? 1 : !string.IsNullOrEmpty(AddonReader.PlayerReader.Target)? 6: 2,
                        Name = "Player",
                        MapId = this.AddonReader.PlayerReader.ZoneId,
                        Spot = this.AddonReader.PlayerReader.PlayerLocation
                    });
                    updatePlayerPostion.Reset();
                    updatePlayerPostion.Restart();
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
                    this.pather.DrawLines();

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

        public void InitialiseBot(string? classProfile, string? pathProfile)
        {
            SelectedClassProfile = classProfile;
            SelectedPath = pathProfile;

            try
            {
                ClassConfig = ReadClassConfiguration(classProfile, pathProfile);
            }
            catch(Exception e)
            {
                logger.LogError(e.Message);
                return;
            }

            var blacklist = this.ClassConfig.Mode != Mode.Grind ? new NoBlacklist() : (IBlacklist)new Blacklist(AddonReader.PlayerReader, ClassConfig.NPCMaxLevels_Above, ClassConfig.NPCMaxLevels_Below, ClassConfig.Blacklist, logger);

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

        private ClassConfiguration ReadClassConfiguration(string? classProfileFile, string? pathProfileFile)
        {
            if(string.IsNullOrEmpty(classProfileFile))
            {
                throw new Exception("Class profile cannot be null!");
            }

            ClassConfiguration classConfig;
            var requirementFactory = new RequirementFactory(AddonReader.PlayerReader, AddonReader.BagReader, logger);

            if(!classProfileFile.ToLower().Contains(AddonReader.PlayerReader.PlayerClass.ToString().ToLower()))
            {
                throw new Exception("Not allowed to load other class profile!");
            }

            var classFilename = $"../json/class/{classProfileFile}";
            if (File.Exists(classFilename))
            {
                classConfig = JsonConvert.DeserializeObject<ClassConfiguration>(File.ReadAllText(classFilename));
                classConfig.Initialise(AddonReader.PlayerReader, requirementFactory, logger);
                
                if(!string.IsNullOrEmpty(pathProfileFile))
                {
                    classConfig.OverrideBasePathFile(pathProfileFile);
                }

                if (!File.Exists($"../json/path/{classConfig.PathFilename}"))
                {
                    if (!string.IsNullOrEmpty(pathProfileFile))
                        throw new Exception($"The `{classConfig.PathFilename}` path file does not exists!");
                    else
                        throw new Exception($"The `{classProfileFile}` contains not existing `{classConfig.PathFilename}` path file!");
                }
                else
                {
                    logger.LogDebug($"Loaded Class Profile `{classProfileFile}` with Path Profile `{classConfig.PathFilename}`.");
                }

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
                this.GoapAgent?.AvailableGoals.ToList().ForEach(goal => goal.OnActionEvent(this, new ActionEventArgs(GoapKey.abort, true)));
            }
        }

        public void Shutdown()
        {
            this.Enabled = false;
        }

        public void LoadClassProfile(string classProfileFileName)
        {
            StopBot();
            InitialiseBot(classProfileFileName, SelectedPath);

            ProfileLoaded?.Invoke(this, EventArgs.Empty);
        }

        public List<string> ClassFileList()
        {
            DirectoryInfo directory = new DirectoryInfo("../Json/class/");
            var list = directory.GetFiles().Select(i => i.Name).ToList();
            list.Sort(new NaturalStringComparer());
            list.Insert(0, String.Empty);
            return list;
        }

        public List<string> PathFileList()
        {
            DirectoryInfo directory = new DirectoryInfo("../Json/path/");
            var list = directory.GetFiles().Select(i => i.Name).ToList();
            list.Sort(new NaturalStringComparer());
            list.Insert(0, "Use Class Profile Default");
            return list;
        }

        public void LoadPathProfile(string pathProfileFileName)
        {
            StopBot();
            InitialiseBot(SelectedClassProfile, pathProfileFileName);

            ProfileLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
}