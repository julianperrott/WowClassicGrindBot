using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Extensions;

namespace Core.Goals
{
    public class ParallelGoal : GoapGoal
    {
        public override float CostOfPerformingAction => 3f;

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly StopMoving stopMoving;
        private readonly Wait wait;
        private readonly PlayerReader playerReader;

        private readonly CastingHandler castingHandler;
        private readonly MountHandler mountHandler;

        public ParallelGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, StopMoving stopMoving, List<KeyAction> keysConfig, CastingHandler castingHandler, MountHandler mountHandler)
        {
            this.logger = logger;
            this.input = input;

            this.stopMoving = stopMoving;
            this.wait = wait;
            this.playerReader = playerReader;

            this.castingHandler = castingHandler;
            this.mountHandler = mountHandler;

            AddPrecondition(GoapKey.incombat, false);

            keysConfig.ForEach(key => Keys.Add(key));
        }

        public override bool CheckIfActionCanRun()
        {
            return Keys.Any(key => key.CanRun());
        }

        public override async ValueTask OnEnter()
        {
            if (Keys.Any(k => k.StopBeforeCast))
            {
                stopMoving.Stop();
                wait.Update(1);

                if (mountHandler.IsMounted())
                {
                    mountHandler.Dismount();
                    wait.Update(1);
                }
            }

            await AsyncExt.Loop(Keys, (KeyAction key) =>
            {
                var pressed = castingHandler.CastIfReady(key, key.DelayBeforeCast);
                key.ResetCooldown();
                key.SetClicked();
                return Task.CompletedTask;
            });

            bool wasDrinkingOrEating = playerReader.Buffs.Drinking || playerReader.Buffs.Eating;

            DateTime startTime = DateTime.UtcNow;
            while ((playerReader.Buffs.Drinking || playerReader.Buffs.Eating || playerReader.IsCasting) && !playerReader.Bits.PlayerInCombat)
            {
                wait.Update(1);

                if (playerReader.Buffs.Drinking && playerReader.Buffs.Eating)
                {
                    if (playerReader.ManaPercentage > 98 && playerReader.HealthPercent > 98) { break; }
                }
                else if (playerReader.Buffs.Drinking)
                {
                    if (playerReader.ManaPercentage > 98) { break; }
                }
                else if (playerReader.Buffs.Eating)
                {
                    if (playerReader.HealthPercent > 98) { break; }
                }

                if ((DateTime.UtcNow - startTime).TotalSeconds >= 25)
                {
                    logger.LogInformation($"Waited (25s) long enough for {Name}");
                    break;
                }
            }

            if (wasDrinkingOrEating)
            {
                input.TapStandUpKey();
            }
        }

        public override ValueTask PerformAction()
        {
            return ValueTask.CompletedTask;
        }
    }
}