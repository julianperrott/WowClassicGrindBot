using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PullTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;

        public PullTargetAction(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.pulled, false);
            AddPrecondition(GoapKey.withinpullrange, true);
            AddEffect(GoapKey.pulled, true);
        }



        public override float CostOfPerformingAction { get => 4f; }

        public override bool CheckIfActionCanRun()
        {
            return true;
            //return this.playerReader.Target.ToLower().StartsWith("g") || this.playerReader.Target.ToLower().StartsWith("c");
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
            // approach
            await this.wowProcess.KeyPress(ConsoleKey.H,501);

            // stop approach
            await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 401);

            if (playerReader.SpellInRange.Charge)
            {
                await this.wowProcess.KeyPress(ConsoleKey.D1, 401);
            }
            else if(playerReader.SpellInRange.ShootGun)
            {
                await this.wowProcess.KeyPress(ConsoleKey.D0, 401);
            }
        }

        public override void ResetBeforePlanning()
        {
            
        }
    }
}
