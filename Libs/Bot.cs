using Libs.Actions;
using Libs.GOAP;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace Libs
{
    public class Bot
    {
        private GoapAction? currentAction;
        private HashSet<GoapAction> availableActions = new HashSet<GoapAction>();
        private readonly WowData wowData;
        private readonly PlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        public readonly GoapAgent Agent;
        public readonly FollowRouteAction followRouteAction;
        public readonly WalkToCorpseAction walkToCorpseAction;
        public readonly NpcNameFinder npcNameFinder;
        public readonly StuckDetector stuckDetector;
        private ILogger logger;
        private Blacklist blacklist;

        public ClassConfiguration? classConfig;

        public delegate void ScreenChangeDelegate(object sender, ScreenChangeEventArgs args);
        public event ScreenChangeDelegate? OnScreenChanged;

        public readonly RouteInfo RouteInfo;

        public bool Active { get; set; }

        public Bot(WowData wowData, ILogger logger)
        {
            this.logger = logger;
            this.wowData = wowData;
            this.blacklist = new Blacklist(wowData.PlayerReader);

            this.Agent = new GoapAgent(wowData.PlayerReader, this.availableActions, this.blacklist, logger);

            var classFilename = $"D:\\GitHub\\WowPixelBot\\{wowData.PlayerReader.PlayerClass.ToString()}.json";
            if (File.Exists(classFilename))
            {
                classConfig = JsonConvert.DeserializeObject<ClassConfiguration>(File.ReadAllText(classFilename));
            }

            List<WowPoint> pathPoints, spiritPath;
            GetPaths(wowData, out pathPoints, out spiritPath);

            this.playerDirection = new PlayerDirection(wowData.PlayerReader, GetWowProcess, logger);
            this.stopMoving = new StopMoving(GetWowProcess, wowData.PlayerReader, logger);
            this.npcNameFinder = new NpcNameFinder(GetWowProcess, wowData.PlayerReader, logger);

            this.stuckDetector = new StuckDetector(wowData.PlayerReader, GetWowProcess, playerDirection, stopMoving, logger);
            this.followRouteAction = new FollowRouteAction(wowData.PlayerReader, GetWowProcess, playerDirection, pathPoints, stopMoving, npcNameFinder, this.blacklist, logger, stuckDetector);
            this.walkToCorpseAction = new WalkToCorpseAction(wowData.PlayerReader, GetWowProcess, playerDirection, spiritPath, pathPoints, stopMoving, logger, stuckDetector);
            this.RouteInfo = new RouteInfo(pathPoints, spiritPath, this.followRouteAction, this.walkToCorpseAction);
        }

        private void GetPaths(WowData wowData, out List<WowPoint> pathPoints, out List<WowPoint> spiritPath)
        {
            string pathText = string.Empty;
            string spiritText = string.Empty;
            int step = 2;
            bool thereAndBack = false;

            if (this.classConfig != null)
            {
                pathText = File.ReadAllText(classConfig.PathFilename);
                thereAndBack = classConfig.PathThereAndBack;
                if (string.IsNullOrEmpty(classConfig.SpiritPathFilename))
                {
                    classConfig.SpiritPathFilename = classConfig.PathFilename;
                }
                spiritText = File.ReadAllText(classConfig.SpiritPathFilename);
                step = classConfig.PathReduceSteps ? 2 : 1;
            }
            else

            {
                //switch (wowData.PlayerReader.PlayerClass)
                //{
                //    case PlayerClassEnum.Warrior:
                //        pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\EPL_57.json");
                //        spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\EPL_57_SpiritHealer.json");
                //        break;
                //    case PlayerClassEnum.Rogue:
                //        pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\52_Tanaris.json");
                //        spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\52_Tanaris_SpiritHealer.json");
                //        break;
                //    case PlayerClassEnum.Priest:
                //        pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_52.json");
                //        spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_52_SpiritHealer.json");
                //        break;
                //    case PlayerClassEnum.Druid:
                //        pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_44.json");
                //        thereAndBack = false;
                //        spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_44_SpiritHealer.json");
                //        step = 2;
                //        break;
                //    case PlayerClassEnum.Paladin:
                //        pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\30_ThousandNeedles.json");
                //        thereAndBack = false;
                //        spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\30_ThousandNeedles_SpirirtHealer.json");
                //        step = 2;
                //        break;
                //    case PlayerClassEnum.Mage:
                //        pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\7_Human.json");
                //        thereAndBack = true;
                //        spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\7_Human.json");
                //        step = 1;
                //        break;
                //}
            }

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
            catch(Exception ex)
            {
                logger.LogError(ex.Message);
            }
            Thread.Sleep(1);

        }

        public async Task DoWork()
        {
            this.currentAction = followRouteAction;

            await GetWowProcess.KeyPress(ConsoleKey.F3, 400); // clear target

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

            this.availableActions.Add(followRouteAction);
            this.availableActions.Add(this.walkToCorpseAction);
            this.availableActions.Add(new TargetDeadAction(GetWowProcess, wowData.PlayerReader, npcNameFinder, logger));
            this.availableActions.Add(new ApproachTargetAction(GetWowProcess, wowData.PlayerReader, stopMoving, npcNameFinder, logger, this.stuckDetector));

            if (this.classConfig != null)
            {
                if (this.classConfig.Loot)
                {
                    this.availableActions.Add(new LootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
                    this.availableActions.Add(new PostKillLootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
                }

                try
                {
                    var genericCombat = new GenericCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger, classConfig);
                    this.availableActions.Add(genericCombat);
                    this.availableActions.Add(new GenericPullAction(GetWowProcess, wowData.PlayerReader, npcNameFinder, stopMoving, logger, genericCombat, classConfig, this.stuckDetector));

                    foreach (var item in classConfig.Adhoc.Sequence)
                    {
                        this.availableActions.Add(new AdhocAction(GetWowProcess, wowData.PlayerReader, stopMoving, item, genericCombat, logger));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }

            }
            else
            {
                throw new ArgumentOutOfRangeException("Class config not loaded");
                //AddClassSpecificActions();
            }

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

        //private void AddClassSpecificActions()
        //{
        //    this.availableActions.Add(new LootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
        //    this.availableActions.Add(new PostKillLootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
        //    this.availableActions.Add(new TimedPressAKeyAction(GetWowProcess, stopMoving, ConsoleKey.F5, 313, logger, "Delete stuff"));
        //    this.availableActions.Add(new UseHealingPotionAction(GetWowProcess, wowData.PlayerReader, logger));

        //    switch (wowData.PlayerReader.PlayerClass)
        //    {
        //        case PlayerClassEnum.Warrior:
        //            this.availableActions.Add(new WarriorCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new BuffAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new EatOrBandageAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new BuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D0), () => wowData.PlayerReader.Buffs.WellFed, logger, "Well Fed"));
        //            break;

        //        case PlayerClassEnum.Rogue:
        //            this.availableActions.Add(new RogueCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new TimedPressAKeyAction(GetWowProcess, stopMoving, ConsoleKey.F6, 3600, logger, "Equip dagger"));
        //            this.availableActions.Add(new BuffAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new EatOrBandageAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            break;

        //        case PlayerClassEnum.Priest:
        //            this.availableActions.Add(new PriestCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new DrinkAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D1), () => wowData.PlayerReader.Buffs.Fortitude, 70, logger, "Fortitude"));
        //            this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D2), () => wowData.PlayerReader.Buffs.InnerFire, 70, logger, "Inner Fire"));
        //            this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D7), () => wowData.PlayerReader.Buffs.DivineSpirit, 70, logger, "Divine Spirit"));
        //            this.availableActions.Add(new BuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.OemMinus), () => wowData.PlayerReader.Buffs.ManaRegeneration, logger, "Well Fed")); // "Nightfin Soup"));
        //            break;

        //        case PlayerClassEnum.Druid:
        //            this.availableActions.Add(new DruidCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new DrinkAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
        //            this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D1), () => wowData.PlayerReader.Buffs.MarkOfTheWild, 70, logger, "Mark of the Wild"));
        //            this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D3), () => wowData.PlayerReader.Buffs.Thorns, 70, logger, "Thorns"));
        //            this.availableActions.Add(new BuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D7), () => wowData.PlayerReader.Buffs.WellFed, logger, "Well Fed"));
        //            break;
        //    }

        //    if (!this.availableActions.Any(c => c as PullTargetAction != null))
        //    {
        //        var combatAction = this.availableActions.First(c => c as CombatActionBase != null) as CombatActionBase;
        //        if (combatAction == null) { throw new Exception("Didn't find combat action"); }
        //        this.availableActions.Add(new ClassPullTargetAction(GetWowProcess, wowData.PlayerReader, npcNameFinder, stopMoving, logger, combatAction, this.stuckDetector));
        //    }
        //}

        public async Task PressKey(ConsoleKey key)
        {
            if (wowData.PlayerReader.PlayerClass == PlayerClassEnum.Druid && wowData.PlayerReader.Druid_ShapeshiftForm != ShapeshiftForm.None)
            {
                await GetWowProcess.KeyPress(ConsoleKey.F8, 500);
            }

            await GetWowProcess.KeyPress(key, 500);
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
                    catch(Exception ex)
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

        private WowProcess? wowProcess;

        public WowProcess GetWowProcess
        {
            get
            {
                if (this.wowProcess == null)
                {
                    this.wowProcess = new WowProcess(logger);
                }
                return this.wowProcess;
            }
        }
    }
}
