using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class PostKillLootGoal : LootGoal
    {
        public override float CostOfPerformingAction { get => 4.5f; }

        public PostKillLootGoal(ILogger logger, ConfigurableInput input, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameFinder npcNameFinder)
            : base(logger, input, playerReader, bagReader, stopMoving, classConfiguration, npcNameFinder)
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