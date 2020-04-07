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
        private ILogger logger;
        private Blacklist blacklist;

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

            string pathText = string.Empty;
            string spiritText = string.Empty;
            int step = 2;
            bool thereAndBack = false;

            switch (wowData.PlayerReader.PlayerClass)
            {
                case PlayerClassEnum.Warrior:
                    pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_52.json");
                    spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_52_SpiritHealer.json");
                    break;
                case PlayerClassEnum.Rogue:
                    pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_52.json");
                    spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_52_SpiritHealer.json");
                    break;
                case PlayerClassEnum.Priest:
                    pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_47.json");
                    spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_44_SpiritHealer.json");
                    break;
                case PlayerClassEnum.Druid:
                    pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi_31.json");
                    thereAndBack = true;
                    spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi_31_SpiritHealer.json");
                    step = 2;
                    break;
            }

            var pathPoints2 = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);

            var pathPoints = new List<WowPoint>();
            for (int i = 0; i < pathPoints2.Count; i += step)
            {
                if (i < pathPoints2.Count)
                {
                    pathPoints.Add(pathPoints2[i]);
                }
            }

            if(thereAndBack)
            {
                var reversePoints = pathPoints.ToList();
                reversePoints.Reverse();
                pathPoints.AddRange(reversePoints);
            }

            pathPoints.Reverse();
            var spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);

            this.playerDirection = new PlayerDirection(wowData.PlayerReader, GetWowProcess, logger);
            this.stopMoving = new StopMoving(GetWowProcess, wowData.PlayerReader, logger);
            this.npcNameFinder = new NpcNameFinder(GetWowProcess, logger);
            this.followRouteAction = new FollowRouteAction(wowData.PlayerReader, GetWowProcess, playerDirection, pathPoints, stopMoving, npcNameFinder, this.blacklist, logger);
            this.walkToCorpseAction = new WalkToCorpseAction(wowData.PlayerReader, GetWowProcess, playerDirection, spiritPath, pathPoints, stopMoving, logger);

            this.RouteInfo = new RouteInfo(pathPoints, spiritPath, this.followRouteAction, this.walkToCorpseAction);
        }

        internal void DoScreenshot()
        {
            var rect = GetWowProcess.GetWindowRect();
            using (var screenshot = new DirectBitmap(rect.right, rect.bottom, 0 ,0))
            {
                screenshot.CaptureScreen();
                this.OnScreenChanged?.Invoke(this, new ScreenChangeEventArgs(screenshot.ToBase64()));
                Thread.Sleep(1);
            }
        }

        public async Task DoWork()
        {
            await GetWowProcess.KeyPress(ConsoleKey.F3, 400); // clear target

            this.currentAction = followRouteAction;

            this.availableActions.Clear();
            this.availableActions.Add(followRouteAction);
            this.availableActions.Add(new LootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
            this.availableActions.Add(new PostKillLootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving, logger));
            this.availableActions.Add(new TargetDeadAction(GetWowProcess, wowData.PlayerReader, npcNameFinder, logger));
            this.availableActions.Add(this.walkToCorpseAction);
            this.availableActions.Add(new UseHealingPotionAction(GetWowProcess, wowData.PlayerReader, logger));
            this.availableActions.Add(new TimedPressAKeyAction(GetWowProcess, stopMoving, ConsoleKey.F5, 313, logger, "Delete stuff"));
            this.availableActions.Add(new ApproachTargetAction(GetWowProcess, wowData.PlayerReader, stopMoving, npcNameFinder, logger));

            switch (wowData.PlayerReader.PlayerClass)
            {
                case PlayerClassEnum.Warrior:
                    this.availableActions.Add(new WarriorCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new BuffAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new EatOrBandageAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new BuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D7), () => wowData.PlayerReader.Buffs.WellFed, logger, "Well Fed"));
                    break;

                case PlayerClassEnum.Rogue:
                    this.availableActions.Add(new RogueCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new TimedPressAKeyAction(GetWowProcess, stopMoving, ConsoleKey.F6, 3600, logger, "Equip dagger"));
                    this.availableActions.Add(new BuffAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new EatOrBandageAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    break;

                case PlayerClassEnum.Priest:
                    this.availableActions.Add(new PriestCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new DrinkAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D1), () => wowData.PlayerReader.Buffs.Fortitude, 70, logger, "Fortitude"));
                    this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D2), () => wowData.PlayerReader.Buffs.InnerFire, 70, logger, "Inner Fire"));
                    this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D7), () => wowData.PlayerReader.Buffs.DivineSpirit, 70, logger, "Divine Spirit"));
                    this.availableActions.Add(new BuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.OemMinus), () => wowData.PlayerReader.Buffs.ManaRegeneration, logger, "Well Fed")); // "Nightfin Soup"));
                    break;

                case PlayerClassEnum.Druid:
                    this.availableActions.Add(new DruidCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new DrinkAction(GetWowProcess, wowData.PlayerReader, stopMoving, logger));
                    this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D1), () => wowData.PlayerReader.Buffs.MarkOfTheWild, 70, logger, "Mark of the Wild"));
                    this.availableActions.Add(new ManaBuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D3), () => wowData.PlayerReader.Buffs.Thorns, 70, logger, "Thorns"));
                    this.availableActions.Add(new BuffPressAKeyAction(GetWowProcess, wowData.PlayerReader, stopMoving, () => PressKey(ConsoleKey.D7), () => wowData.PlayerReader.Buffs.WellFed, logger, "Well Fed"));
                    break;
            }

            var combatAction = this.availableActions.First(c => c as CombatActionBase != null) as CombatActionBase;
            if (combatAction==null) { throw new Exception("Didn't find combat action"); }
            this.availableActions.Add(new PullTargetAction(GetWowProcess, wowData.PlayerReader, npcNameFinder, stopMoving, logger, combatAction));

            this.availableActions.ToList().ForEach(a => 
            {
                a.ActionEvent += this.Agent.OnActionEvent;
                a.ActionEvent += npcNameFinder.OnActionEvent;
                a.ActionEvent += this.OnActionEvent;

                // tell other action about my actions
                this.availableActions.ToList().ForEach(b =>
                {
                    if (b!=a) { a.ActionEvent += b.OnActionEvent; }
                });
            });

            while (Active)
            {
                await GoapPerformAction();
            }

            await stopMoving.Stop();
            logger.LogInformation("Stopped!");

        }

        public async Task PressKey(ConsoleKey key)
        {
            if (wowData.PlayerReader.PlayerClass == PlayerClassEnum.Druid && wowData.PlayerReader.ShapeshiftForm != 0)
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
