using Libs.GOAP;
using Libs.NpcFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PullTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StopMoving stopMoving;

        public PullTargetAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.npcNameFinder = npcNameFinder;
            this.stopMoving = stopMoving;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.pulled, false);
            AddPrecondition(GoapKey.withinpullrange, true);
            AddEffect(GoapKey.pulled, true);
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override async Task PerformAction()
        {
            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            // approach
            await this.wowProcess.KeyPress(ConsoleKey.H, 501);

            // stop approach
            await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 401);

            Debug.WriteLine($"Can shoot gun: {playerReader.SpellInRange.ShootGun}");

            var npcCount = this.npcNameFinder.CountNpc();
            Debug.WriteLine($"Npc count = {npcCount}");

            if (playerReader.SpellInRange.Charge && npcCount < 2)
            {
                await this.wowProcess.KeyPress(ConsoleKey.D1, 401);
            }
            else if (playerReader.SpellInRange.ShootGun)
            {
                await this.wowProcess.KeyPress(ConsoleKey.D9, 1000);
                await Task.Delay(3000);
            }
        }
    }
}
