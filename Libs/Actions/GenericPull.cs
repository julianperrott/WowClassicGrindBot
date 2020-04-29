using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class GenericPullAction : PullTargetAction
    {
        private readonly ClassConfiguration classConfiguration;

        public GenericPullAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving, ILogger logger, CombatActionBase combatAction, ClassConfiguration classConfiguration, StuckDetector stuckDetector)
        : base(wowProcess, playerReader, npcNameFinder, stopMoving, logger, combatAction, stuckDetector)
        {
            this.classConfiguration = classConfiguration;
        }

        public override bool ShouldStopBeforePull => this.classConfiguration.Pull.Sequence.Count>0;

        public override async Task<bool> Pull()
        {
            this.combatAction.AddsExist = npcNameFinder.PotentialAddsExist;

            bool hasCast = false;

            //stop combat
            await this.wowProcess.KeyPress(ConsoleKey.F10, 300);
            this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;

            foreach (var item in this.classConfiguration.Pull.Sequence.Where(i => i != null))
            {
                var sleepBeforeFirstCast = item.StopBeforeCast && !hasCast && 500> item.DelayBeforeCast ? 500 : item.DelayBeforeCast;

                var success = await this.combatAction.CastIfReady(item,this, sleepBeforeFirstCast);
                hasCast = hasCast || success;

                if (!this.playerReader.HasTarget)
                {
                    return false;
                }

                if (hasCast && item.WaitForWithinMelleRange)
                {
                    await this.WaitForWithinMelleRange();
                }
            }

            // Wait for combat
            if (hasCast)
            {
                for (int i = 0; i < 40; i++)
                {
                    // wait for combat, for mob to be targetting me or have suffered damage or 2 seconds to have elapsed.
                    // sometimes after casting a ranged attack, we can be in combat before the attack has landed.
                    if (this.playerReader.PlayerBitValues.PlayerInCombat &&
                        (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer || this.playerReader.TargetHealthPercentage < 99 || i > 20))
                    {
                        return true;
                    }

                    await Task.Delay(100);
                }
            }
            
            return this.playerReader.PlayerBitValues.PlayerInCombat;
        }
    }
}
