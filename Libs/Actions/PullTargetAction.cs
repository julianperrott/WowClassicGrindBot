using Libs.GOAP;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
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
        private ILogger logger;

        public PullTargetAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.npcNameFinder = npcNameFinder;
            this.stopMoving = stopMoving;
            this.logger = logger;

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

            logger.LogInformation($"Stop approach");
            await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 301);


            logger.LogInformation($"Can shoot gun: {playerReader.SpellInRange.Warrior_ShootGun}");

            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Mount();
            }

            bool pulled = await Pull();
            if (!pulled)
            {
                // approach
                if (playerReader.PlayerBitValues.IsMounted)
                {
                    await wowProcess.Mount();
                }
                await this.wowProcess.KeyPress(ConsoleKey.H, 301);
            }
        }

        public async Task<bool> Pull()
        {
            var npcCount = this.npcNameFinder.CountNpc();
            logger.LogInformation($"Npc count = {npcCount}");

            bool pulled = false;

            pulled = playerReader.PlayerClass switch
                {
                    PlayerClassEnum.Warrior => await WarriorPull(npcCount),
                    PlayerClassEnum.Rogue => await RoguePull(npcCount),
                    _ => false
                };

            return false;
        }

        private async Task<bool> WarriorPull(int npcCount)
        {
            if (playerReader.SpellInRange.Warrior_Charge && npcCount < 2)
            {
                logger.LogInformation($"Charging");
                await this.wowProcess.KeyPress(ConsoleKey.D1, 401);
                return true;
            }

            if (playerReader.SpellInRange.Warrior_ShootGun && npcCount > 1)
            {
                // stop approach
                logger.LogInformation($"Stop approach");
                await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 301);

                logger.LogInformation($"Shooting Gun");
                await Task.Delay(300);
                await this.wowProcess.KeyPress(ConsoleKey.D9, 1000);

                await WaitForWithinMelleRange();
                return true;
            }

            return false;
        }

        private async Task WaitForWithinMelleRange()
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                if (playerReader.WithInMeleeRange) { return; }
            }
        }

        private async Task<bool> RoguePull(int npcCount)
        {
            if (playerReader.SpellInRange.Rogue_Throw)
            {
                // stop approach
                logger.LogInformation($"Stop approach");
                await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 301);

                logger.LogInformation($"Throwing Knife");
                await Task.Delay(300);
                await this.wowProcess.KeyPress(ConsoleKey.D9, 1000);

                await WaitForWithinMelleRange();
                return true;
            }

            return false;
        }
    }
}
