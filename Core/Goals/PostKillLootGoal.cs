using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class PostKillLootGoal : LootGoal
    {
        public override float CostOfPerformingAction { get => 4.5f; }

        public PostKillLootGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameTargeting npcNameTargeting, CombatUtil combatUtil, IPlayerDirection playerDirection)
            : base(logger, input, wait, addonReader, stopMoving, classConfiguration, npcNameTargeting, combatUtil, playerDirection)
        {
        }

        public override void AddPreconditions()
        {
            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.shouldloot, true);
        }

        public override async ValueTask PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
            await base.PerformAction();
        }
    }
}