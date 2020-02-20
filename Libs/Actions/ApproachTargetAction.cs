using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class ApproachTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        
        public ApproachTargetAction(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            AddPrecondition(GoapKey.inmeleerange, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);
        }

        public override float CostOfPerformingAction { get => 8f; }

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

            wowProcess.KeyUp(ConsoleKey.LeftArrow);
            await Task.Delay(1);
            wowProcess.KeyUp(ConsoleKey.RightArrow);
            await Task.Delay(1);
            wowProcess.KeyUp(ConsoleKey.UpArrow);
            await Task.Delay(1);
            await this.wowProcess.KeyPress(ConsoleKey.H, 501);
        }

        public override void ResetBeforePlanning()
        {
            
        }
    }
}
