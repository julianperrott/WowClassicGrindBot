using Libs.Actions;
using Libs.GOAP;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Libs
{
    public class Bot
    {
        private GoapAction? currentAction;
        private HashSet<GoapAction> availableActions = new HashSet<GoapAction>();
        private readonly WowData wowData;
        private readonly PlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly FollowRouteAction followRouteAction;
        private readonly WalkToCorpseAction walkToCorpseAction;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StuckDetector stuckDetector;
        private ILogger logger;
        private Blacklist blacklist;
        private WowProcess wowProcess;
        private ClassConfiguration classConfig = new ClassConfiguration();

        public delegate void ScreenChangeDelegate(object sender, ScreenChangeEventArgs args);
        public event ScreenChangeDelegate? OnScreenChanged;
        public readonly GoapAgent Agent;
        public readonly RouteInfo RouteInfo;
        public bool Active { get; set; }
        public bool PotentialAddsExist => npcNameFinder.PotentialAddsExist;

        public Bot(WowData wowData, ILogger logger)
        {
            this.logger = logger;
            this.wowData = wowData;
            
            var classFilename = $"D:\\GitHub\\WowPixelBot\\{wowData.PlayerReader.PlayerClass.ToString()}.json";
            if (File.Exists(classFilename))
            {
                classConfig = JsonConvert.DeserializeObject<ClassConfiguration>(File.ReadAllText(classFilename));
                classConfig.Initialise(wowData.PlayerReader, logger);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Class config file not found {classFilename}");
            }

            this.blacklist = new Blacklist(wowData.PlayerReader, classConfig.NPCMaxLevels_Above,classConfig.NPCMaxLevels_Below);
            this.wowProcess = new WowProcess(logger);

            this.Agent = new GoapAgent(wowData.PlayerReader, this.availableActions, this.blacklist, logger);


            List<WowPoint> pathPoints, spiritPath;
            GetPaths(wowData, out pathPoints, out spiritPath);

            this.playerDirection = new PlayerDirection(wowData.PlayerReader, wowProcess, logger);
            this.stopMoving = new StopMoving(wowProcess, wowData.PlayerReader, logger);
            this.npcNameFinder = new NpcNameFinder(wowProcess, wowData.PlayerReader, logger);

            this.stuckDetector = new StuckDetector(wowData.PlayerReader, wowProcess, playerDirection, stopMoving, logger);
            this.followRouteAction = new FollowRouteAction(wowData.PlayerReader, wowProcess, playerDirection, pathPoints, stopMoving, npcNameFinder, this.blacklist, logger, stuckDetector, classConfig);
            this.walkToCorpseAction = new WalkToCorpseAction(wowData.PlayerReader, wowProcess, playerDirection, spiritPath, pathPoints, stopMoving, logger, stuckDetector);
            this.RouteInfo = new RouteInfo(pathPoints, spiritPath, this.followRouteAction, this.walkToCorpseAction);
        }

        private void GetPaths(WowData wowData, out List<WowPoint> pathPoints, out List<WowPoint> spiritPath)
        {
            string pathText = string.Empty;
            string spiritText = string.Empty;
            int step = 2;
            bool thereAndBack = false;

            pathText = File.ReadAllText(classConfig.PathFilename);
            thereAndBack = classConfig.PathThereAndBack;
            if (string.IsNullOrEmpty(classConfig.SpiritPathFilename))
            {
                classConfig.SpiritPathFilename = classConfig.PathFilename;
            }
            spiritText = File.ReadAllText(classConfig.SpiritPathFilename);
            step = classConfig.PathReduceSteps ? 2 : 1;

            var pathPoints2 = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);

            pathPoints = new List<WowPoint>();
            for (int i = 0; i < pathPoints2.Count; i += step)
            {
                if (i < pathPoints2.Count)
                {
                    pathPoints.Add(pathPoints2[i]);
                }
            }

            if (thereAndBack)
            {
                var reversePoints = pathPoints.ToList();
                reversePoints.Reverse();
                pathPoints.AddRange(reversePoints);
            }

            pathPoints.Reverse();
            spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);
        }

        internal void DoScreenshot()
        {
            try
            {
                var npcs = this.npcNameFinder.RefreshNpcPositions();

                if (npcs.Count > 0)
                {
                    var bitmap = this.npcNameFinder.Screenshot.Bitmap;

                    using (var gr = Graphics.FromImage(bitmap))
                    {
                        var margin = 10;

                        using (var redPen = new Pen(Color.Red, 2))
                        {
                            npcs.ForEach(n => gr.DrawRectangle(redPen, new Rectangle(n.Min.X - margin, n.Min.Y - margin, margin + n.Max.X - n.Min.X, margin + n.Max.Y - n.Min.Y)));

                            using (var whitePen = new Pen(Color.White, 3))
                            {
                                npcs.ForEach(n => gr.DrawEllipse(n.IsAdd ? whitePen : redPen, new Rectangle(n.ClickPoint.X - (margin / 2), n.ClickPoint.Y - (margin / 2), margin, margin)));
                            }
                        }
                    }
                }

                this.OnScreenChanged?.Invoke(this, new ScreenChangeEventArgs(this.npcNameFinder.Screenshot.ToBase64()));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            Thread.Sleep(1);
        }

        public async Task DoWork()
        {
            this.currentAction = followRouteAction;

            await wowProcess.KeyPress(ConsoleKey.F3, 400); // clear target

            CreateActions();

            while (Active)
            {
                await GoapPerformAction();
            }

            await stopMoving.Stop();
            logger.LogInformation("Stopped!");
        }

        private void CreateActions()
        {
            this.availableActions.Clear();

            this.availableActions.Add(this.followRouteAction);
            this.availableActions.Add(this.walkToCorpseAction);
            this.availableActions.Add(new TargetDeadAction(wowProcess, wowData.PlayerReader, npcNameFinder, logger));
            this.availableActions.Add(new ApproachTargetAction(wowProcess, wowData.PlayerReader, stopMoving, npcNameFinder, logger, this.stuckDetector));

            if (this.classConfig.Loot)
            {
                this.availableActions.Add(new LootAction(wowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
                this.availableActions.Add(new PostKillLootAction(wowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
            }

            try
            {
                var genericCombat = new GenericCombatAction(wowProcess, wowData.PlayerReader, stopMoving, logger, classConfig, this.playerDirection);
                this.availableActions.Add(genericCombat);
                this.availableActions.Add(new GenericPullAction(wowProcess, wowData.PlayerReader, npcNameFinder, stopMoving, logger, genericCombat, classConfig, this.stuckDetector));

                foreach (var item in classConfig.Adhoc.Sequence)
                {
                    this.availableActions.Add(new AdhocAction(wowProcess, wowData.PlayerReader, stopMoving, item, genericCombat, logger));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            // hookup events between actions
            this.availableActions.ToList().ForEach(a =>
            {
                a.ActionEvent += this.Agent.OnActionEvent;
                a.ActionEvent += npcNameFinder.OnActionEvent;
                a.ActionEvent += this.OnActionEvent;

                // tell other action about my actions
                this.availableActions.ToList().ForEach(b =>
                {
                    if (b != a) { a.ActionEvent += b.OnActionEvent; }
                });
            });
        }

        public void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.abort)
            {
                logger.LogInformation($"Abort from: {sender.GetType().Name}");

                var location = wowData.PlayerReader.PlayerLocation;
                wowProcess?.Hearthstone();
                Active = false;
            }
        }

        private async Task GoapPerformAction()
        {
            if (this.Agent != null)
            {
                if (this.wowData.PlayerReader.PlayerBitValues.ItemsAreBroken)
                {
                    OnActionEvent(this, new ActionEvent(GoapKey.abort, true));
                }

                var newAction = await this.Agent.GetAction();

                if (newAction != null)
                {
                    if (newAction != this.currentAction)
                    {
                        this.currentAction?.DoReset();
                        this.currentAction = newAction;
                        logger.LogInformation("---------------------------------");
                        logger.LogInformation($"New Plan= {newAction.GetType().Name}");
                    }

                    try
                    {
                        await newAction.PerformAction();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"PerformAction on {newAction.GetType().Name}");
                    }
                }
                else
                {
                    logger.LogInformation($"New Plan= NULL");
                    Thread.Sleep(500);
                }
            }
        }
    }
}