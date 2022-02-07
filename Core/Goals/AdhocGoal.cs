using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class AdhocGoal : GoapGoal
    {
        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly Wait wait;
        private readonly StopMoving stopMoving;
        private readonly PlayerReader playerReader;
        
        private readonly KeyAction key;
        private readonly CastingHandler castingHandler;
        private readonly MountHandler mountHandler;
        public override float CostOfPerformingAction => key.Cost;

        public override string Name => Keys.Count == 0 ? base.Name : Keys[0].Name;

        public AdhocGoal(ILogger logger, ConfigurableInput input, Wait wait, KeyAction key, PlayerReader playerReader, StopMoving stopMoving, CastingHandler castingHandler, MountHandler mountHandler)
        {
            this.logger = logger;
            this.input = input;
            this.wait = wait;
            this.stopMoving = stopMoving;
            this.playerReader = playerReader;
            this.key = key;
            this.castingHandler = castingHandler;
            this.mountHandler = mountHandler;

            if (key.InCombat == "false")
            {
                AddPrecondition(GoapKey.incombat, false);
            }
            else if (key.InCombat == "true")
            {
                AddPrecondition(GoapKey.incombat, true);
            }

            Keys.Add(key);
        }

        public override bool CheckIfActionCanRun() => key.CanRun();

        public override ValueTask OnEnter()
        {
            if (key.StopBeforeCast)
            {
                stopMoving.Stop();
                wait.Update(1);
            }

            if (mountHandler.IsMounted())
            {
                mountHandler.Dismount();
                wait.Update(1);
            }

            castingHandler.CastIfReady(key, key.DelayBeforeCast);

            bool wasDrinkingOrEating = playerReader.Buffs.Drinking || playerReader.Buffs.Eating;

            DateTime startTime = DateTime.Now;

            while ((playerReader.Buffs.Drinking || playerReader.Buffs.Eating || playerReader.IsCasting) && !playerReader.Bits.PlayerInCombat)
            {
                wait.Update(1);

                if (playerReader.Buffs.Drinking)
                {
                    if (playerReader.ManaPercentage > 98) { break; }
                }
                else if (playerReader.Buffs.Eating && !key.Requirement.Contains("Well Fed"))
                {
                    if (playerReader.HealthPercent > 98) { break; }
                }
                else if (!key.CanRun())
                {
                    break;
                }

                if ((DateTime.Now - startTime).TotalSeconds > 25)
                {
                    logger.LogInformation($"Waited (25s) long enough for {key.Name}");
                    break;
                }
            }

            if (wasDrinkingOrEating)
            {
                input.TapStopKey();
            }

            return base.OnEnter();
        }

        public override ValueTask PerformAction()
        {
            return ValueTask.CompletedTask;
        }
    }
}
