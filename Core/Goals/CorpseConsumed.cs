using System.Threading.Tasks;
using Core.GOAP;
using Microsoft.Extensions.Logging;

namespace Core.Goals
{
    public class CorpseConsumed : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.7f; }

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

        public override ValueTask OnEnter()
        {
            goapAgentState.DecKillCount();
            logger.LogInformation($"----- Corpse consumed. Remaining: {goapAgentState.LastCombatKillCount}");

            SendActionEvent(new ActionEventArgs(GoapKey.consumecorpse, false));

            return base.OnEnter();
        }

        public override ValueTask PerformAction()
        {
            return ValueTask.CompletedTask;
        }
    }
}