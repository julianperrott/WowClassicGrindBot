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

namespace Libs
{
    public class Bot
    {
        private GoapAction? currentAction;
        private HashSet<GoapAction> availableActions = new HashSet<GoapAction>();
        private PlayerReader playerReader;
        private PlayerDirection playerDirection;
        private StopMoving stopMoving;
        public GoapAgent Agent;
        public FollowRouteAction followRouteAction;

        public RouteInfo RouteInfo;

        public bool Active { get; set; }

        public Bot(PlayerReader playerReader)
        {
            this.playerReader = playerReader;
            this.Agent = new GoapAgent(playerReader, this.availableActions);

            var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi_37.json");
            var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi_37_SpiritHealer.json");

            var pathPoints = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);
            pathPoints.Reverse();
            var spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);

            RouteInfo=new RouteInfo(pathPoints, spiritPath);

            this.playerDirection = new PlayerDirection(playerReader, WowProcess);
            this.stopMoving = new StopMoving(WowProcess, playerReader);
            this.followRouteAction = new FollowRouteAction(playerReader, WowProcess, playerDirection, RouteInfo.PathPoints, stopMoving);
        }

        public async Task DoWork()
        {
            this.currentAction = followRouteAction;

            var killMobAction = new KillTargetAction(WowProcess, playerReader, stopMoving);
            var pullTargetAction = new PullTargetAction(WowProcess, playerReader);
            var approachTargetAction = new ApproachTargetAction(WowProcess, playerReader, stopMoving);
            var lootAction = new LootAction(WowProcess, playerReader, stopMoving);
            var healAction = new HealAction(WowProcess, playerReader, stopMoving);

            this.availableActions.Add(followRouteAction);
            this.availableActions.Add(killMobAction);
            this.availableActions.Add(pullTargetAction);
            this.availableActions.Add(approachTargetAction);
            this.availableActions.Add(lootAction);
            this.availableActions.Add(healAction);
            this.availableActions.Add(new TargetDeadAction(WowProcess, playerReader));
            this.availableActions.Add(new WalkToCorpseAction(playerReader, WowProcess, playerDirection, RouteInfo.SpiritPath, RouteInfo.PathPoints, stopMoving));

            while (Active)
            {
                await GoapPerformAction();
            }

            await stopMoving.Stop();
            Debug.WriteLine("Stopped!");

        }

        private async Task GoapPerformAction()
        {
            if (this.Agent != null)
            {
                var newAction = await this.Agent.GetAction();

                if (newAction != null)
                {
                    if (newAction != this.currentAction)
                    {
                        this.currentAction?.DoReset();
                        this.currentAction = newAction;
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

        public WowProcess WowProcess
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
