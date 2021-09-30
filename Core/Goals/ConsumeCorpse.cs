using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.GOAP;
using Microsoft.Extensions.Logging;

namespace Core.Goals
{
    class ConsumeCorpse : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.7f; }
        public override bool Repeatable => false;

        private readonly ILogger logger;
        private readonly PlayerReader playerReader;

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
            playerReader.DecrementKillCount();
            logger.LogInformation("----- Consumed a corpse. Remaining:" + playerReader.LastCombatKillCount);

            playerReader.ConsumeCorpse();
            SendActionEvent(new ActionEventArgs(GoapKey.consumecorpse, false));

            await Task.Delay(10);
        }
    }
}