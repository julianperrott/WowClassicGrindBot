using Core.GOAP;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
using System.Numerics;
using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class LastTargetLoot : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.3f; }
        public override bool Repeatable => false;

        private ILogger logger;
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        private readonly Wait wait;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly CombatUtil combatUtil;

        private bool debug = true;
        private int lastLoot;

        public LastTargetLoot(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, StopMoving stopMoving, CombatUtil combatUtil)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;
            this.bagReader = addonReader.BagReader;

            this.combatUtil = combatUtil;
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.shouldloot, true);
            AddEffect(GoapKey.shouldloot, false);
        }

        public override async ValueTask OnEnter()
        {
            await base.OnEnter();

            if (bagReader.BagsFull)
            {
                logger.LogWarning("Inventory is full");
                SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
            }
        }

        public override async ValueTask PerformAction()
        {
            int lastHealth = playerReader.HealthCurrent;
            var lastPosition = playerReader.PlayerLocation;
            lastLoot = playerReader.LastLootTime;

            await stopMoving.Stop();
            combatUtil.Update();

            await input.TapLastTargetKey($"{GetType().Name}: No corpse name found - check last dead target exists");
            await wait.Update(1);
            if (playerReader.HasTarget)
            {
                if (playerReader.Bits.TargetIsDead)
                {
                    await input.TapInteractKey($"{GetType().Name}: Found last dead target");
                    await wait.Update(1);

                    (bool foundTarget, bool moved) = await combatUtil.FoundTargetWhileMoved();
                    if (foundTarget)
                    {
                        Log("Goal interrupted!");
                        return;
                    }

                    if (moved)
                    {
                        await input.TapInteractKey($"{GetType().Name}: Last dead target double");
                    }
                }
                else
                {
                    await input.TapClearTarget($"{GetType().Name}: Don't attack the target!");
                }
            }

            await GoalExit();
        }

        private async Task GoalExit()
        {
            if (!await wait.Interrupt(1000, () => lastLoot != playerReader.LastLootTime))
            {
                Log($"Loot Successfull");
            }
            else
            {
                Log($"Loot Failed");
            }

            lastLoot = playerReader.LastLootTime;

            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));

            if (playerReader.HasTarget && playerReader.Bits.TargetIsDead)
            {
                await input.TapClearTarget($"{GetType().Name}: Exit Goal");
                await wait.Update(1);
            }
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{GetType().Name}: {text}");
            }
        }
    }
}