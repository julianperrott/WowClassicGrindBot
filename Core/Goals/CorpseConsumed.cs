using System.Threading.Tasks;
using Core.GOAP;
using Microsoft.Extensions.Logging;

namespace Core.Goals
{
    public class CorpseConsumed : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.7f; }
        public override bool Repeatable => false;

        private readonly ILogger logger;
        private readonly GoapAgentState goapAgentState;

        public CorpseConsumed(ILogger logger, GoapAgentState goapAgentSate)
        {
            this.logger = logger;
            this.goapAgentState = goapAgentSate;

            AddPrecondition(GoapKey.dangercombat, false);
            AddPrecondition(GoapKey.consumecorpse, true);

            AddEffect(GoapKey.consumecorpse, false);
        }

        public override ValueTask PerformAction()
        {
            goapAgentState.DecKillCount();
            logger.LogInformation($"----- Corpse consumed. Remaining: {goapAgentState.LastCombatKillCount}");

            SendActionEvent(new ActionEventArgs(GoapKey.consumecorpse, false));
            return ValueTask.CompletedTask;
        }
    }
}