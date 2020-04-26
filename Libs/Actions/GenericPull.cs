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

            foreach (var item in this.classConfiguration.Pull.Sequence.Where(i => i != null))
            {
                var sleepBeforeFirstCast = item.StopBeforeCast && !hasCast && 500> item.DelayBeforeCast ? 500 : item.DelayBeforeCast;

                var success = await this.combatAction.CastIfReady(item, sleepBeforeFirstCast);
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

            if (hasCast)
            {
                for (int i = 0; i < 40; i++)
                {
                    if (this.playerReader.PlayerBitValues.PlayerInCombat)
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
