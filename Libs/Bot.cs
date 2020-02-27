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
        public GoapAgent Agent;

        public Bot(PlayerReader playerReader)
        {
            this.playerReader = playerReader;
            this.Agent = new GoapAgent(playerReader, this.availableActions);
        }

        public async Task DoWork()
        {
            var playerDirection = new PlayerDirection(playerReader, WowProcess);

            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200210195132.json");
            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200215184939.json");
            //var text = File.ReadAllText(@"D:\GitHub\WowPixelBot\Path_20200217215324.json");
            //var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\ThousandNeedles.json");
            //var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\ThousandNeedlesSpiritHealer.json");

            var pathText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi.json");
            var spiritText = File.ReadAllText(@"D:\GitHub\WowPixelBot\Arathi_SpritHealer.json");

            var pathPoints = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);
            var pathPointsReversed = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);
            pathPointsReversed.Reverse();
            var pathThereAndBack = pathPoints.Concat(pathPointsReversed).ToList();

            var spiritPoints = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);

            var followRouteAction = new FollowRouteAction(playerReader, WowProcess, playerDirection, pathThereAndBack);
            this.currentAction = followRouteAction;

            var killMobAction = new KillTargetAction(WowProcess, playerReader);
            var pullTargetAction = new PullTargetAction(WowProcess, playerReader);
            var approachTargetAction = new ApproachTargetAction(WowProcess, playerReader);
            var lootAction = new LootAction(WowProcess, playerReader);
            var healAction = new HealAction(WowProcess, playerReader);

            this.availableActions.Add(followRouteAction);
            this.availableActions.Add(killMobAction);
            this.availableActions.Add(pullTargetAction);
            this.availableActions.Add(approachTargetAction);
            this.availableActions.Add(lootAction);
            this.availableActions.Add(healAction);
            this.availableActions.Add(new TargetDeadAction(WowProcess, playerReader));
            this.availableActions.Add(new WalkToCorpseAction(playerReader, WowProcess, playerDirection, spiritPoints, pathPoints));

            while (true)
            {
                await GoapPerformAction();
            }
        }

        private async Task GoapPerformAction()
        {
            var newAction = this.Agent?.GetAction();

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
