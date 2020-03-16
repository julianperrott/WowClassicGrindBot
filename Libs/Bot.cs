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
        private List<string> blacklist = new List<string> { "THORKA", "ZARICO" };

        public readonly RouteInfo RouteInfo;

        public bool Active { get; set; }

        public Bot(WowData wowData)
        {
            this.wowData = wowData;
            this.Agent = new GoapAgent(wowData.PlayerReader, this.availableActions, this.blacklist);

            var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_44.json");
            var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Tanaris_44_SpiritHealer.json");

            var pathPoints = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);

            //var pathPoints = new List<WowPoint>();
            //for (int i=0;i< pathPoints2.Count;i+=2)
            //{
            //    if (i < pathPoints2.Count)
            //    {
            //        pathPoints.Add(pathPoints2[i]);
            //    }
            //}

            pathPoints.Reverse();
            var spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);

            this.playerDirection = new PlayerDirection(wowData.PlayerReader, GetWowProcess);
            this.stopMoving = new StopMoving(GetWowProcess, wowData.PlayerReader);
            this.npcNameFinder = new NpcNameFinder(GetWowProcess);
            this.followRouteAction = new FollowRouteAction(wowData.PlayerReader, GetWowProcess, playerDirection, pathPoints, stopMoving, npcNameFinder, this.blacklist);
            this.walkToCorpseAction = new WalkToCorpseAction(wowData.PlayerReader, GetWowProcess, playerDirection, spiritPath, pathPoints, stopMoving);

            this.RouteInfo = new RouteInfo(pathPoints, spiritPath, this.followRouteAction, this.walkToCorpseAction);
        }

        public async Task DoWork()
        {
            this.currentAction = followRouteAction;

            this.availableActions.Clear();
            this.availableActions.Add(followRouteAction);
            this.availableActions.Add(new PullTargetAction(GetWowProcess, wowData.PlayerReader, npcNameFinder, stopMoving));
            this.availableActions.Add(new ApproachTargetAction(GetWowProcess, wowData.PlayerReader, stopMoving, npcNameFinder));
            this.availableActions.Add(new LootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving));
            this.availableActions.Add(new PostKillLootAction(GetWowProcess, wowData.PlayerReader, wowData.bagReader, stopMoving));
            this.availableActions.Add(new HealAction(GetWowProcess, wowData.PlayerReader, stopMoving));
            this.availableActions.Add(new TargetDeadAction(GetWowProcess, wowData.PlayerReader, npcNameFinder));
            this.availableActions.Add(this.walkToCorpseAction);
            this.availableActions.Add(new UseHealingPotionAction(GetWowProcess, wowData.PlayerReader));
            this.availableActions.Add(new BuffAction(GetWowProcess, wowData.PlayerReader, stopMoving));
            this.availableActions.Add(new PressAKeyAction(GetWowProcess, stopMoving, ConsoleKey.F5, 313));
            this.availableActions.Add(new PressAKeyAction(GetWowProcess, stopMoving, ConsoleKey.F6, 3600));

            this.availableActions.Add(wowData.PlayerReader.PlayerClass switch
            {
                PlayerClassEnum.Warrior=> new WarriorCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving),
                PlayerClassEnum.Rogue => new RogueCombatAction(GetWowProcess, wowData.PlayerReader, stopMoving),
                _ => throw new ArgumentOutOfRangeException("Player class")
            });

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
            Debug.WriteLine("Stopped!");

        }

        public void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.abort)
            {
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
                        Debug.WriteLine("---------------------------------");
                        Debug.WriteLine($"New Plan= {newAction.GetType().Name}");
                    }

                    await newAction.PerformAction();
                }
                else
                {
                    Debug.WriteLine($"New Plan= NULL");
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
                    this.wowProcess = new WowProcess();
                }
                return this.wowProcess;
            }
        }
    }
}
