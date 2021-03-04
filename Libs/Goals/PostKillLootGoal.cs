using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class PostKillLootGoal : LootGoal
    {
        public override float CostOfPerformingAction { get => 4.5f; }

        public PostKillLootGoal(ILogger logger, WowInput wowInput, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameFinder npcNameFinder)
            : base(logger, wowInput, playerReader, bagReader, stopMoving, classConfiguration, npcNameFinder)
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