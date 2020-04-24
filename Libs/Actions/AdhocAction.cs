using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class AdhocAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly StopMoving stopMoving;
        private readonly PlayerReader playerReader;
        private readonly ILogger logger;
        private readonly KeyConfiguration key;
        private readonly CombatActionBase combatAction;

        public AdhocAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, KeyConfiguration key, CombatActionBase combatAction, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.stopMoving = stopMoving;
            this.playerReader = playerReader;
            this.key = key;
            this.logger = logger;
            this.combatAction = combatAction;

            if (key.InCombat == "false")
            {
                AddPrecondition(GoapKey.incombat, false);
            }
            else if (key.InCombat == "true")
            {
                AddPrecondition(GoapKey.incombat, true);
            }
        }

        public override float CostOfPerformingAction { get => key.Cost; }

        public override async Task PerformAction()
        {
            if (key.StopBeforeCast)
            {
                await this.stopMoving.Stop();

                if (playerReader.PlayerBitValues.IsMounted)
                {
                    await wowProcess.Dismount();
                }
                await Task.Delay(1000);
            }

            await this.combatAction.CastIfReady(key);

            bool wasDrinkingOrEating = this.playerReader.Buffs.Drinking || this.playerReader.Buffs.Eating;

            int seconds = 0;

            while ((this.playerReader.Buffs.Drinking || this.playerReader.Buffs.Eating || this.playerReader.IsCasting) && !this.playerReader.PlayerBitValues.PlayerInCombat)
            {
                await Task.Delay(1000);
                seconds++;
                this.logger.LogInformation($"Waiting for {key.Name}");

                if (this.playerReader.Buffs.Drinking)
                {
                    if (this.playerReader.ManaPercentage > 98) { break; }
                }
                else if (this.playerReader.Buffs.Eating && this.key.Requirement != "Well Fed")
                {
                    if (this.playerReader.HealthPercent > 98) { break; }
                }
                else if (this.HasRequirement())
                {
                    break;
                }

                if (seconds > 20)
                {
                    this.logger.LogInformation($"Waited long enough for {key.Name}");
                    break;
                }
            }

            if (HasRequirement())
            {
                this.logger.LogInformation($"I have the buff {key.Name}");
            }
            else
            {
                this.logger.LogInformation($"I don't have the buff {key.Name}");
            }

            if (wasDrinkingOrEating)
            {
                await wowProcess.TapStopKey(); // stand up
            }
        }

        public override bool CheckIfActionCanRun()
        {
            return this.combatAction.CanRun(key) && !HasRequirement();
        }

        public bool HasRequirement()
        {
            return this.combatAction.MeetsRequirement(this.key);
        }

        public override string Description()
        {
            if (HasRequirement())
            {
                return key.ToString();
            }
            else
            {
                var canRun = this.combatAction.CanRun(key);
                var hasEnoughManaText = this.playerReader.ManaCurrent > this.key.MinMana ? string.Empty : "(MANA)";
                return $"{key.ToString()} {hasEnoughManaText} Can Run:{canRun}".Replace(" ", " ");
            }
        }
    }
}
