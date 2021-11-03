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
        private readonly GoapAgentState goapAgentState;

        public ConsumeCorpse(ILogger logger, GoapAgentState goapAgentSate)
        {
            this.logger = logger;
            this.goapAgentState = goapAgentSate;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.consumecorpse, true);

            AddEffect(GoapKey.consumecorpse, false);
        }

        public override async Task PerformAction()
        {
            goapAgentState.DecrementKillCount();
            logger.LogInformation("----- Consumed a corpse. Remaining:" + goapAgentState.LastCombatKillCount);

            goapAgentState.ConsumeCorpse();
            SendActionEvent(new ActionEventArgs(GoapKey.consumecorpse, false));

            await Task.Delay(10);
        }
    }
}