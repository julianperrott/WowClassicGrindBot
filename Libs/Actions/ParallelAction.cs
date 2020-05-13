using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Libs.Actions
{
    public class ParallelAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly StopMoving stopMoving;
        private readonly PlayerReader playerReader;
        private readonly ILogger logger;
        private readonly CastingHandler castingHandler;

        public ParallelAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, List<KeyConfiguration> keys, CastingHandler castingHandler, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.stopMoving = stopMoving;
            this.playerReader = playerReader;
            this.logger = logger;
            this.castingHandler = castingHandler;

            AddPrecondition(GoapKey.incombat, false);

            keys.ForEach(key => this.Keys.Add(key));
        }

        public override bool CheckIfActionCanRun()
        {
            return this.Keys.Any(key => key.CanRun());
        }

        public override float CostOfPerformingAction  { get => 3f; }

        public override async Task PerformAction()
        {
           
            if (this.Keys.Any(k=>k.StopBeforeCast))
            {
                await this.stopMoving.Stop();

                if (playerReader.PlayerBitValues.IsMounted)
                {
                    await wowProcess.Dismount();
                }
                await Task.Delay(1000);
            }

            this.Keys.ForEach(async key =>
            {
                var pressed = await this.castingHandler.CastIfReady(key, this);
                key.ResetCooldown();
                key.SetClicked();
            });

            bool wasDrinkingOrEating = this.playerReader.Buffs.Drinking || this.playerReader.Buffs.Eating;

            int seconds = 0;

            while ((this.playerReader.Buffs.Drinking || this.playerReader.Buffs.Eating || this.playerReader.IsCasting) && !this.playerReader.PlayerBitValues.PlayerInCombat)
            {
                await Task.Delay(1000);
                seconds++;
                this.logger.LogInformation($"Waiting for {Name}");

                if (this.playerReader.Buffs.Drinking && this.playerReader.Buffs.Eating)
                {
                    if (this.playerReader.ManaPercentage > 98 && this.playerReader.HealthPercent > 98) { break; }
                }
                else if(this.playerReader.Buffs.Drinking )
                {
                    if (this.playerReader.ManaPercentage > 98 ) { break; }
                }
                else if (this.playerReader.Buffs.Eating)
                {
                    if (this.playerReader.HealthPercent > 98) { break; }
                }

                if (seconds > 25)
                {
                    this.logger.LogInformation($"Waited long enough for {Name}");
                    break;
                }
            }

            if (wasDrinkingOrEating)
            {
                await wowProcess.TapStopKey(); // stand up
            }
        }

        //public override string Name => this.Keys.Count == 0 ? base.Name : string.Join(", ", this.Keys.Select(k => k.Name));
    }
}