using Core.Database;
using Core.GOAP;
using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class LootGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.4f; }
        public override bool Repeatable => false;

        private ILogger logger;
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        private readonly Wait wait;
        private readonly AreaDB areaDb;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private readonly NpcNameTargeting npcNameTargeting;
        private readonly CombatUtil combatUtil;

        private bool debug = true;
        private int lastLoot;

        public LootGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameTargeting npcNameTargeting, CombatUtil combatUtil, AreaDB areaDb)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            this.areaDb = areaDb;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            
            this.classConfiguration = classConfiguration;
            this.npcNameTargeting = npcNameTargeting;
            this.combatUtil = combatUtil;
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.shouldloot, true);
            AddEffect(GoapKey.shouldloot, false);
        }

        public override async Task OnEnter()
        {
            await base.OnEnter();

            if (bagReader.BagsFull)
            {
                logger.LogWarning("Inventory is full");
                SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
            }

            Log($"Search for {NpcNames.Corpse}");
            npcNameTargeting.ChangeNpcType(NpcNames.Corpse);
        }

        public override async Task PerformAction()
        {
            lastLoot = playerReader.LastLootTime;

            await stopMoving.Stop();
            combatUtil.Update();

            await npcNameTargeting.WaitForNUpdate(2);
            bool foundCursor = await npcNameTargeting.FindBy(CursorType.Loot);
            if (foundCursor)
            {
                Log("Found corpse - clicked");
                (bool notFoundTarget, double elapsedMs) = await wait.InterruptTask(200, () => playerReader.TargetId != 0);
                if (!notFoundTarget)
                {
                    Log($"Found target after {elapsedMs}ms");
                }

                CheckForSkinning();

                (bool foundTarget, bool moved) = await combatUtil.FoundTargetWhileMoved();
                if (foundTarget)
                {
                    Log("Interrupted!");
                    return;
                }

                if (moved)
                {
                    await input.TapInteractKey($"{GetType().Name}: Had to move so interact again");
                    await wait.Update(1);
                }
            }
            else
            {
                await input.TapLastTargetKey($"{GetType().Name}: No corpse name found - check last dead target exists");
                await wait.Update(1);
                if (playerReader.HasTarget)
                {
                    if(playerReader.Bits.TargetIsDead)
                    {
                        CheckForSkinning();

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
                        await input.TapClearTarget($"{GetType().Name}: Dont attak the target!");
                    }
                }
            }

            await GoalExit();
        }

        private void CheckForSkinning()
        {
            if (classConfiguration.Skin)
            {
                var targetSkinnable = !playerReader.Unskinnable;

                if (areaDb.CurrentArea != null && areaDb.CurrentArea.skinnable != null)
                {
                    targetSkinnable = areaDb.CurrentArea.skinnable.Contains(playerReader.TargetId);
                    Log($"{playerReader.TargetId} is skinnable? {targetSkinnable}");
                }
                else
                {
                    Log($"{playerReader.TargetId} was not found in the database!");
                }

                Log($"Should skin ? {targetSkinnable}");
                AddEffect(GoapKey.shouldskin, targetSkinnable);

                SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, targetSkinnable));
            }
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

                SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, false));
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