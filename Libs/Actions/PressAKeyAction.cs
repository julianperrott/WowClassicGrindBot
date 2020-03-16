using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PressAKeyAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly StopMoving stopMoving;
        private readonly ConsoleKey key;
        private readonly int secondsCooldown;
        private DateTime LastPressed = DateTime.Now.AddDays(-1);

        public PressAKeyAction(WowProcess wowProcess, StopMoving stopMoving, ConsoleKey key, int secondsCooldown)
        {
            this.wowProcess = wowProcess;
            this.stopMoving = stopMoving;
            this.key = key;
            this.secondsCooldown = secondsCooldown;

            AddPrecondition(GoapKey.incombat, false);
        }

        public override float CostOfPerformingAction { get => 1f; }

        public override async Task PerformAction()
        {
            await this.stopMoving.Stop();

            await wowProcess.KeyPress(key, 500);

            LastPressed = DateTime.Now;
        }

        public override bool CheckIfActionCanRun()
        {
            return (DateTime.Now - LastPressed).TotalSeconds > secondsCooldown;
        }

        public override string Description()
        {
            if (!CheckIfActionCanRun())
            {
                var timespan = LastPressed.AddSeconds(secondsCooldown) - DateTime.Now;
                return $" - {key.ToString()} - {DateTime.Now.Date.AddSeconds(timespan.TotalSeconds).ToString("mm:ss")}";
            }
            else
            {
                return $" - {key.ToString()} - Pending";
            }
        }
    }
}
