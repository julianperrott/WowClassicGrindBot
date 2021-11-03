using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class PostKillLootGoal : LootGoal
    {
        public override float CostOfPerformingAction { get => 4.5f; }

        public PostKillLootGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameTargeting npcNameTargeting, CombatUtil combatUtil)
            : base(logger, input, wait, addonReader, stopMoving, classConfiguration, npcNameTargeting, combatUtil)
        {
        }

        public override void AddPreconditions()
        {
            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.shouldloot, true);
        }

        public override async Task PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
            await base.PerformAction();
        }
    }
}