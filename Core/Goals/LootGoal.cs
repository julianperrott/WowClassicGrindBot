using Core.GOAP;
using Core.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class LootGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.4f; }

        private ILogger logger;
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private readonly NpcNameFinder npcNameFinder;
        private readonly CombatUtil combatUtil;

        private bool debug = true;
        private long LastLoot;

        public LootGoal(ILogger logger, ConfigurableInput input, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving,  ClassConfiguration classConfiguration, NpcNameFinder npcNameFinder, CombatUtil combatUtil)
        {
            this.logger = logger;
            this.input = input;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            
            this.classConfiguration = classConfiguration;
            this.npcNameFinder = npcNameFinder;
            this.combatUtil = combatUtil;

            LastLoot = playerReader.LastLootTime;
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.shouldloot, true);
            AddEffect(GoapKey.shouldloot, false);
        }

        public override bool CheckIfActionCanRun()
        {
            return !bagReader.BagsFull && playerReader.ShouldConsumeCorpse;
        }

        public override async Task PerformAction()
        {
            combatUtil.Update();

            Log("Search for corpse");
            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Corpse);

            await stopMoving.Stop();
            await npcNameFinder.WaitForNUpdate(1);

            bool foundCursor = await npcNameFinder.FindByCursorType(Cursor.CursorClassification.Loot);
            if (foundCursor)
            {
                Log("Found corpse - clicked");
                await playerReader.WaitForNUpdate(1);

                CheckForSkinning();

                (bool foundTarget, bool moved) = await combatUtil.FoundTargetWhileMoved();
                if (foundTarget)
                {
                    Log("Interrupted!");
                    SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
                    SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                    SendActionEvent(new ActionEventArgs(GoapKey.hastarget, true));
                    SendActionEvent(new ActionEventArgs(GoapKey.pulled, true));
                    return;
                }

                if(moved) 
                {
                    await input.TapInteractKey($"{GetType().Name}: Had to move so interact again");
                }
            }
            else
            {
                await input.TapLastTargetKey($"{GetType().Name}: No corpse name found - check last dead target exists");
                await playerReader.WaitForNUpdate(1);
                if(playerReader.HasTarget)
                {
                    if(playerReader.PlayerBitValues.TargetIsDead)
                    {
                        CheckForSkinning();

                        await input.TapInteractKey($"{GetType().Name}: Found last dead target");
                        await playerReader.WaitForNUpdate(1);

                        (bool foundTarget, bool moved) = await combatUtil.FoundTargetWhileMoved();
                        if (foundTarget)
                        {
                            Log("Goal interrupted!");
                            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
                            SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                            SendActionEvent(new ActionEventArgs(GoapKey.hastarget, true));
                            SendActionEvent(new ActionEventArgs(GoapKey.pulled, true));
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
                Log($"Should skin ? {targetSkinnable}");
                AddEffect(GoapKey.shouldskin, targetSkinnable);
                SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, targetSkinnable));
            }
        }

        private async Task GoalExit()
        {
            if(!await Wait(500, () => LastLoot != playerReader.LastLootTime))
            {
                Log($"Loot Successfull");
            }
            else
            {
                Log($"Loot Failed");
            }

            LastLoot = playerReader.LastLootTime;

            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
            await Task.Delay(1);

            if (!classConfiguration.Skin)
            {
                npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Enemy);
            }

            if (playerReader.HasTarget && playerReader.PlayerBitValues.TargetIsDead)
            {
                await input.TapClearTarget($"{GetType().Name}: Exit Goal");
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