using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class HouseKeepingAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly StopMoving stopMoving;
        private readonly PlayerReader playerReader;
        private readonly ILogger logger;
        private readonly KeyConfiguration key;
        //private DateTime LastPressed = DateTime.Now.AddDays(-1);
        private Func<bool> HasBuff;
        private readonly CombatActionBase combatAction;

        public HouseKeepingAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving,KeyConfiguration key, CombatActionBase combatAction, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.stopMoving = stopMoving;
            this.playerReader = playerReader;
            this.key = key;
            this.logger = logger;
            this.combatAction = combatAction;

            if (string.IsNullOrEmpty(key.Buff))
            {
                this.HasBuff = () => false;
            }
            else
            {
                this.HasBuff = playerReader.GetBuffFunc(key.Name, key.Buff);
            }

            AddPrecondition(GoapKey.incombat, false);
        }

        public override float CostOfPerformingAction { get => 18f; }

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
                else if (this.playerReader.Buffs.Eating && this.key.Buff != "Well Fed")
                {
                    if (this.playerReader.HealthPercent > 98) { break; }
                }
                else if (this.HasBuff())
                {
                    break;
                }

                if (seconds > 20)
                {
                    this.logger.LogInformation($"Waited long enough for {key.Name}");
                    break;
                }
            }

            if (HasBuff())
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
            return this.combatAction.CanRun(key, false) && !HasBuff();
        }

        public override string Description()
        {
            if (HasBuff())
            {
                return $" - {key.Name} - Has buff";
            }
            else
            {
                //var timespan = LastPressed.AddSeconds(key.Cooldown) - DateTime.Now;
                //var timeCoolDownText = !CheckIfActionCanRun() ? DateTime.Now.Date.AddSeconds(timespan.TotalSeconds).ToString("mm:ss") : string.Empty;
                var canRun = this.combatAction.CanRun(key, false);
                var hasEnoughManaText = this.playerReader.ManaCurrent > this.key.ManaRequirement ? string.Empty : "(MANA)";
                var hasDesiredBuffText = HasBuff() ? "OK" : "Need Buff";
                return $" - {key.Name} - {hasDesiredBuffText} {hasEnoughManaText} CanRun:{canRun}".Replace(" ", " ");
            }
        }
    }
}
