using Libs.GOAP;
using Libs.Looting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PostKillLootAction : LootAction
    {
        public PostKillLootAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving) : base(wowProcess, playerReader, stopMoving)
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
            //await base.wowProcess.KeyPress(ConsoleKey.Spacebar, 500);
            await Task.Delay(1000);
            await base.PerformAction();
        }
    }
}