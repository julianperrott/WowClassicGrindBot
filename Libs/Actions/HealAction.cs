using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class HealAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;

        public HealAction(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.shouldheal, true);
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

        private ConsoleKey Eat => this.playerReader.ActionBarUseable_73To96.HotKey7 ? ConsoleKey.D7 : ConsoleKey.Escape;
        private ConsoleKey Bandage => ConsoleKey.D8;

        public override async Task PerformAction()
        {
            // force stop turning
            wowProcess.KeyUp(ConsoleKey.LeftArrow);
            await Task.Delay(1);
            wowProcess.KeyUp(ConsoleKey.RightArrow);
            await Task.Delay(1);
            wowProcess.KeyUp(ConsoleKey.UpArrow);

            if (this.playerReader.HealthPercent < 40 && Eat != ConsoleKey.Escape)
            {
                await PressKeyAndWait(Eat, 27);
            }
            else
            {
                await PressKeyAndWait(Bandage, 7);
            }
        }

        private async Task PressKeyAndWait(ConsoleKey key, int seconds)
        {
            if (key != ConsoleKey.Escape)
            {
                await wowProcess.KeyPress(key, 500);
            }

            for (int i = 0; i < seconds; i++)
            {
                if (this.playerReader.HealthPercent > 98 || this.playerReader.PlayerBitValues.PlayerInCombat)
                {
                    break;
                }
                await Task.Delay(1000);
            }
        }

        public override void ResetBeforePlanning()
        {
            
        }
    }
}
