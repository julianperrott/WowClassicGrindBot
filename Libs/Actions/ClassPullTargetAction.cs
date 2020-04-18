using Libs.GOAP;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class ClassPullTargetAction : PullTargetAction
    {
        public override bool ShouldStopBeforePull => true;

        public ClassPullTargetAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving, ILogger logger, CombatActionBase combatAction, StuckDetector stuckDetector)
            : base(wowProcess, playerReader, npcNameFinder, stopMoving, logger, combatAction, stuckDetector)
        {

        }

        public override async Task<bool> Pull()
        {
            var potentialAdds=this.npcNameFinder.PotentialAddsExist();

            bool pulled = false;

            if (HasPickedUpAnAdd) { return false; }

            pulled = playerReader.PlayerClass switch
            {
                PlayerClassEnum.Warrior => await WarriorPull(potentialAdds),
                PlayerClassEnum.Rogue => await RoguePull(potentialAdds),
                PlayerClassEnum.Priest => await PriestPull(potentialAdds),
                PlayerClassEnum.Druid => await DruidPull(potentialAdds),
                _ => false
            };

            return pulled;
        }

        private async Task<bool> WarriorPull(bool potentialAdds)
        {
            if (playerReader.SpellInRange.Warrior_Charge && !potentialAdds)
            {
                logger.LogInformation($"Charging");
                await this.wowProcess.KeyPress(ConsoleKey.D1, 401);
                return true;
            }

            if (playerReader.SpellInRange.Warrior_ShootGun && potentialAdds)
            {
                // stop approach
                logger.LogInformation($"Stop approach");
                await wowProcess.TapStopKey();

                logger.LogInformation($"Shooting Gun");
                await Task.Delay(300);
                await this.wowProcess.KeyPress(ConsoleKey.D9, 1000);

                await WaitForWithinMelleRange();
                return true;
            }

            return false;
        }

        private async Task<bool> RoguePull(bool potentialAdds)
        {
            if (playerReader.SpellInRange.Rogue_Throw)
            {
                // stop approach
                logger.LogInformation($"Stop approach");
                await wowProcess.TapStopKey();

                logger.LogInformation($"Throwing Knife");
                await Task.Delay(300);
                await this.wowProcess.KeyPress(ConsoleKey.D9, 1000);

                await WaitForWithinMelleRange();
                return true;
            }

            return false;
        }

        private async Task<bool> DruidPull(bool potentialAdds)
        {
            if (playerReader.PlayerClass == PlayerClassEnum.Druid)
            {
                if (this.playerReader.Druid_ShapeshiftForm != ShapeshiftForm.None)
                {
                    await this.wowProcess.KeyPress(ConsoleKey.F8, 100); // cancelform
                }
            }

            //await this.wowProcess.KeyPress(ConsoleKey.OemPlus, 301);

            if (playerReader.WithInPullRange)
            {
                logger.LogInformation($"Stop approach");
                //await StopAfterH();

                await Task.Delay(600);

                if (HasPickedUpAnAdd) { return false; }

                if (this.playerReader.Druid_ShapeshiftForm != ShapeshiftForm.None)
                {
                    await this.wowProcess.KeyPress(ConsoleKey.F8, 300); // cancelform
                }

                if (this.playerReader.HealthPercent < 75)
                {
                    logger.LogInformation($"Healing");
                    await this.wowProcess.KeyPress(ConsoleKey.D9, 300); // Rejuve
                    while (this.playerReader.HealthPercent < 80 && !this.playerReader.PlayerBitValues.PlayerInCombat)
                    {
                        await Task.Delay(100);
                    }
                }

                if (!playerReader.WithInPullRange)
                {
                    return false;
                }

                if (HasPickedUpAnAdd) { return false; }

                await this.combatAction.PressCastKeyAndWaitForCastToEnd(ConsoleKey.D2,2000);

                if (this.playerReader.WithInCombatRange) { return true; }

                var combatSequence = random.Next(3);
                if (combatSequence != 0)
                {
                    await this.combatAction.PressKey(ConsoleKey.D5); // moonfire
                }

                if (this.playerReader.WithInCombatRange) { return true; }
                await this.combatAction.PressCastKeyAndWaitForCastToEnd(ConsoleKey.D2,2000);
                
                if (this.playerReader.Druid_ShapeshiftForm != ShapeshiftForm.Druid_Bear) // needs bear form
                {
                    await this.wowProcess.KeyPress(ConsoleKey.D4, 300); // bear form

                    for(int i=0;i<20;i++)
                    {
                        if (this.playerReader.WithInCombatRange) { return true; }
                        await Task.Delay(100);
                    }
                }

                return true;
            }

            return false;
        }

        private async Task<bool> PriestPull(bool potentialAdds)
        {
            await this.wowProcess.KeyPress(ConsoleKey.OemPlus, 301);

            if (playerReader.SpellInRange.Priest_MindBlast)
            {
                logger.LogInformation($"Stop approach");

                await Task.Delay(600);

                logger.LogInformation($"Shield");
                if (this.playerReader.HealthPercent < 90)
                {
                    await this.wowProcess.KeyPress(ConsoleKey.D3, 520);
                }

                await Task.Delay(300);

                logger.LogInformation($"Cast Mind Blast");
                await this.combatAction.PressKey(ConsoleKey.D5);

                await Task.Delay(1000);
                logger.LogInformation($"SWP");
                await this.combatAction.PressKey(ConsoleKey.D6);

                // wait for combat
                for (int i = 0; i < 20; i++)
                {
                    if (this.playerReader.PlayerBitValues.PlayerInCombat && this.playerReader.WithInCombatRange)
                    {
                        break;
                    }
                    await Task.Delay(100);
                }

                await Task.Delay(600);

                return true;
            }

            return false;
        }
    }
}