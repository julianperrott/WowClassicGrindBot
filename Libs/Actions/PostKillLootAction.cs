using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PostKillLootAction : LootAction
    {
        public PostKillLootAction(WowProcess wowProcess, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration)
            : base(wowProcess, playerReader, bagReader, stopMoving, logger, classConfiguration)
        {
        }

        protected override void AddPreconditions()
        {
            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.shouldloot, true);
        }

        public override float CostOfPerformingAction { get => 5f; }

        public override async Task PerformAction()
        {
            await Task.Delay(1000);
            await base.PerformAction();
        }
    }
}