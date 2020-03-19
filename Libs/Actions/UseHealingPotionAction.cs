using Libs.GOAP;
using Microsoft.Extensions.Logging;
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
        private ILogger logger;

        private DateTime LastHealed = DateTime.Now.AddDays(-1);

        public UseHealingPotionAction(WowProcess wowProcess, PlayerReader playerReader, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.logger = logger;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.usehealingpotion, true);
        }

        public override float CostOfPerformingAction { get => 1f; }

        public override async Task PerformAction()
        {
            await wowProcess.KeyPress(ConsoleKey.F4, 500);
            LastHealed = DateTime.Now;
            logger.LogInformation("Using healing potion");
        }

        public override bool CheckIfActionCanRun()
        {
            return (DateTime.Now - LastHealed).TotalSeconds > 60;
        }
    }
}
