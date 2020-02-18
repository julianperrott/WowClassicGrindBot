using Libs.GOAP;
using Libs.Looting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Libs.Actions
{
    public class TargetDeadAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly LootWheel lootWheel;
        private bool debug = true;

        public TargetDeadAction(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            lootWheel = new LootWheel(wowProcess);
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

        public override bool CheckIfActionCanRun()
        {
            return true;
        }

        public override bool IsActionDone()
        {
            return false;
        }

        public override bool NeedsToBeInRangeOfTargetToExecute()
        {
            throw new NotImplementedException();
        }

        public override async Task PerformAction()
        {
            Log("Start PerformAction");

            await wowProcess.KeyPress(ConsoleKey.D0, 564);

            Log("End PerformAction");
        }

        public override void ResetBeforePlanning()
        {
        }
    }
}