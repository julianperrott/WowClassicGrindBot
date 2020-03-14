using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class UseHealingPotionAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;

        private DateTime LastHealed = DateTime.Now.AddDays(-1);

        public UseHealingPotionAction(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.usehealingpotion, true);
        }

        public override float CostOfPerformingAction { get => 1f; }

        public override async Task PerformAction()
        {
            await wowProcess.KeyPress(ConsoleKey.F4, 500);
            LastHealed = DateTime.Now;
            Debug.WriteLine("Using healing potion");
        }

        public override bool CheckIfActionCanRun()
        {
            return (DateTime.Now - LastHealed).TotalSeconds > 60;
        }
    }
}
