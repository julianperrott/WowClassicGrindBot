using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class ParallelGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 3f; }

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly StopMoving stopMoving;
        private readonly PlayerReader playerReader;
        
        private readonly CastingHandler castingHandler;

        public ParallelGoal(ILogger logger, ConfigurableInput input, PlayerReader playerReader, StopMoving stopMoving, List<KeyAction> keysConfig, CastingHandler castingHandler)
        {
            this.logger = logger;
            this.input = input;

            this.stopMoving = stopMoving;
            this.playerReader = playerReader;
            
            this.castingHandler = castingHandler;

            AddPrecondition(GoapKey.incombat, false);

            keysConfig.ForEach(key => this.Keys.Add(key));
        }

        public override bool CheckIfActionCanRun()
        {
            return this.Keys.Any(key => key.CanRun());
        }

        public override async Task PerformAction()
        {
            if (this.Keys.Any(k => k.StopBeforeCast))
            {
                await this.stopMoving.Stop();

                if (playerReader.PlayerBitValues.IsMounted)
                {
                    await input.Dismount();
                }
                if (!await Wait(1000, () => playerReader.PlayerBitValues.PlayerInCombat)) return;
            }

            this.Keys.ForEach(async key =>
            {
                var pressed = await this.castingHandler.CastIfReady(key);
                key.ResetCooldown();
                key.SetClicked();
            });

            if (!await Wait(400, () => playerReader.PlayerBitValues.PlayerInCombat)) return;

            bool wasDrinkingOrEating = this.playerReader.Buffs.Drinking || this.playerReader.Buffs.Eating;
            int ticks = 0;

            this.logger.LogInformation($"Waiting for {Name}");
            while ((this.playerReader.Buffs.Drinking || this.playerReader.Buffs.Eating || this.playerReader.IsCasting) && !this.playerReader.PlayerBitValues.PlayerInCombat)
            {
                if (!await Wait(100, () => playerReader.PlayerBitValues.PlayerInCombat)) return;
                ticks++;

                if (this.playerReader.Buffs.Drinking && this.playerReader.Buffs.Eating)
                {
                    if (this.playerReader.ManaPercentage > 98 && this.playerReader.HealthPercent > 98) { break; }
                }
                else if (this.playerReader.Buffs.Drinking)
                {
                    if (this.playerReader.ManaPercentage > 98) { break; }
                }
                else if (this.playerReader.Buffs.Eating)
                {
                    if (this.playerReader.HealthPercent > 98) { break; }
                }

                if (ticks > 250)
                {
                    this.logger.LogInformation($"Waited long enough for {Name}");
                    break;
                }
            }

            if (wasDrinkingOrEating)
            {
                await input.TapStandUpKey(); // stand up
            }
        }
    }
}