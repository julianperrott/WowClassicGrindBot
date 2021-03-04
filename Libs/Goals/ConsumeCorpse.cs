using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Libs.GOAP;
using Microsoft.Extensions.Logging;

namespace Libs.Goals
{
    class ConsumeCorpse : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.7f; }

        private readonly ILogger logger;
        private readonly PlayerReader playerReader;
        private DateTime lastActive = DateTime.Now;

        public ConsumeCorpse(ILogger logger, PlayerReader playerReader)
        {
            this.logger = logger;
            this.playerReader = playerReader;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.consumecorpse, true);

            AddEffect(GoapKey.consumecorpse, false);
        }

        public override bool CheckIfActionCanRun()
        {
            return playerReader.ShouldConsumeCorpse;
        }

        public override async Task PerformAction()
        {
            if((DateTime.Now - lastActive).TotalSeconds > 0.5f)
            {
                playerReader.DecrementKillCount();
                logger.LogInformation("----- Consumed a corpse. Remaining:" + playerReader.LastCombatKillCount);

                playerReader.ConsumeCorpse();
                SendActionEvent(new ActionEventArgs(GoapKey.consumecorpse, false));

                lastActive = DateTime.Now;
            }

            await Task.Delay(0);
        }
    }
}