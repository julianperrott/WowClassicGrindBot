using Core.GOAP;
using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class SkinningGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.6f; }

        public override bool Repeatable => false;

        private ILogger logger;
        private readonly ConfigurableInput input;

        private readonly Wait wait;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly EquipmentReader equipmentReader;
        private readonly NpcNameTargeting npcNameTargeting;
        private readonly CombatUtil combatUtil;

        private int lastLoot;

        public SkinningGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, BagReader bagReader, EquipmentReader equipmentReader, StopMoving stopMoving, NpcNameTargeting npcNameTargeting, CombatUtil combatUtil)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            this.equipmentReader = equipmentReader;

            this.npcNameTargeting = npcNameTargeting;
            this.combatUtil = combatUtil;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.shouldskin, true);

            AddEffect(GoapKey.shouldskin, false);
        }

        public override bool CheckIfActionCanRun()
        {
            return
            (
            bagReader.HasItem(7005) ||
            bagReader.HasItem(12709) ||
            bagReader.HasItem(19901) ||

            equipmentReader.HasItem(7005) ||
            equipmentReader.HasItem(12709) ||
            equipmentReader.HasItem(19901)
            );
        }

        public override async Task OnEnter()
        {
            await base.OnEnter();

            if (bagReader.BagsFull)
            {
                logger.LogWarning("Inventory is full");
                playerReader.NeedSkin = false;
                SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, false));
            }

            npcNameTargeting.ChangeNpcType(NpcNames.Corpse);
        }

        public override async Task PerformAction()
        {
            lastLoot = playerReader.LastLootTime;

            await stopMoving.Stop();
            combatUtil.Update();

            Log($"Try to find {NpcNames.Corpse}");
            await npcNameTargeting.WaitForNUpdate(1);

            int attempts = 1;
            while (attempts < 5)
            {
                if (await combatUtil.EnteredCombat())
                {
                    if (await combatUtil.AquiredTarget())
                    {
                        Log("Interrupted!");
                        return;
                    }
                }

                bool foundCursor = await npcNameTargeting.FindBy(CursorType.Skin);
                if (foundCursor)
                {
                    Log("Found corpse - interacted with right click");
                    await wait.Update(1);

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

                    // wait until start casting
                    await wait.Interrupt(500, () => playerReader.IsCasting);
                    Log("Started casting...");

                    playerReader.LastUIErrorMessage = UI_ERROR.NONE;

                    await wait.Interrupt(3000, () => !playerReader.IsCasting || playerReader.LastUIErrorMessage != UI_ERROR.NONE);
                    Log("Cast finished!");

                    if (playerReader.LastUIErrorMessage != UI_ERROR.ERR_SPELL_FAILED_S)
                    {
                        playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                        Log($"Skinning Successful! {playerReader.LastUIErrorMessage}");
                        await GoalExit();
                        return;
                    }
                    else
                    {
                        Log($"Skinning Failed! Retry... Attempts: {attempts}");
                        attempts++;
                    }
                }
                else
                {
                    Log($"Target is not skinnable - NPC Count: {npcNameTargeting.NpcCount}");
                    await GoalExit();
                    return;
                }
            }

        }

        private async Task GoalExit()
        {
            if (!await wait.Interrupt(1000, () => lastLoot != playerReader.LastLootTime))
            {
                Log($"Skin-Loot Successfull");
            }
            else
            {
                Log($"Skin-Loot Failed");
            }

            lastLoot = playerReader.LastLootTime;

            playerReader.NeedSkin = false;
            SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, false));

            if (playerReader.HasTarget && playerReader.PlayerBitValues.TargetIsDead)
            {
                await input.TapClearTarget();
                await wait.Update(1);
            }
        }

        private void Log(string text)
        {
            logger.LogInformation($"{this.GetType().Name}: {text}");
        }

    }
}
