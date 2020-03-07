using Libs.GOAP;
using Libs.Looting;
using Libs.NpcFinder;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class TargetDeadAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly LootWheel lootWheel;
        private bool debug = true;
        private readonly NpcNameFinder npcFinder;

        public TargetDeadAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcFinder)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.npcFinder = npcFinder;

            lootWheel = new LootWheel(wowProcess, playerReader);

            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, false);
        }

        private void Log(string text)
        {
            if (debug)
            {
                Debug.WriteLine($"{this.GetType().Name}: {text}");
            }
        }

        public override float CostOfPerformingAction { get => 4f; }


        public override async Task PerformAction()
        {
            Log("Start PerformAction");

            this.npcFinder.StopFindingNpcs(10);

            await wowProcess.KeyPress(ConsoleKey.D0, 564);

            Log("End PerformAction");
        }
    }
}