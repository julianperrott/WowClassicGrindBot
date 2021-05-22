using Core.GOAP;
using Core.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class SkinningGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.6f; }

        private ILogger logger;
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly EquipmentReader equipmentReader;
        private readonly NpcNameFinder npcNameFinder;
        private readonly CombatUtil combatUtil;

        public SkinningGoal(ILogger logger, ConfigurableInput input, PlayerReader playerReader, BagReader bagReader, EquipmentReader equipmentReader, StopMoving stopMoving,  NpcNameFinder npcNameFinder, CombatUtil combatUtil)
        {
            this.logger = logger;
            this.input = input;

            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            this.equipmentReader = equipmentReader;

            this.npcNameFinder = npcNameFinder;
            this.combatUtil = combatUtil;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.shouldskin, true);

            AddEffect(GoapKey.shouldskin, false);
        }

        public override bool CheckIfActionCanRun()
        {
            return !bagReader.BagsFull && 
                playerReader.ShouldConsumeCorpse && 
                (
                bagReader.HasItem(7005) ||
                bagReader.HasItem(12709) ||
                bagReader.HasItem(19901) ||

                equipmentReader.HasItem(7005) ||
                equipmentReader.HasItem(12709) ||
                equipmentReader.HasItem(19901)
                );
        }

        public override async Task PerformAction()
        {
            combatUtil.Update();

            Log("Try to find Corpse");
            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Corpse);

            await stopMoving.Stop();
            await npcNameFinder.WaitForNUpdate(1);

            if (await combatUtil.EnteredCombat())
            {
                await combatUtil.AquiredTarget();
                return;
            }

            

            bool skinSuccess = await npcNameFinder.FindByCursorType(Cursor.CursorClassification.Skin);
            if (skinSuccess)
            {
                Log("Found corpse - interacted with right click");
                await playerReader.WaitForNUpdate(1);

                (bool foundTarget, bool moved) = await combatUtil.FoundTargetWhileMoved();
                if (foundTarget)
                {
                    Log("Interrupted!");
                    return;
                }

                if (moved)
                {
                    await input.TapInteractKey($"{GetType().Name}: Had to move so interact again");
                }

                do
                {
                    await playerReader.WaitForNUpdate(1);
                    if (await combatUtil.EnteredCombat())
                    {
                        await combatUtil.AquiredTarget();
                        return;
                    }
                } while (playerReader.IsCasting);

                // Wait for to update the LastUIErrorMessage
                await playerReader.WaitForNUpdate(1);
                var lastError = this.playerReader.LastUIErrorMessage;
                if (lastError != UI_ERROR.ERR_SPELL_FAILED_S)
                {
                    this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    logger.LogDebug("Skinning Successful!");
                    await GoalExit();
                }
                else
                {
                    logger.LogDebug("Skinning Failed! Retry...");
                }
            }
            else
            {
                logger.LogDebug($"Target is not skinnable - NPC Count: {npcNameFinder.NpcCount}");
                await GoalExit();
            }
        }

        async Task GoalExit()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, false));

            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Enemy);

            if (playerReader.HasTarget && playerReader.PlayerBitValues.TargetIsDead)
            {
                await input.TapClearTarget();
            }
        }

        private void Log(string text)
        {
            logger.LogInformation($"{this.GetType().Name}: {text}");
        }

    }
}
