using Libs.GOAP;
using Libs.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class SkinningGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.6f; }

        private ILogger logger;
        private readonly WowInput wowInput;

        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private readonly NpcNameFinder npcNameFinder;
        

        private bool outOfCombat = false;

        public SkinningGoal(ILogger logger, WowInput wowInput, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving,  ClassConfiguration classConfiguration, NpcNameFinder npcNameFinder)
        {
            this.logger = logger;
            this.wowInput = wowInput;

            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            
            this.classConfiguration = classConfiguration;
            this.npcNameFinder = npcNameFinder;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.shouldskin, true);

            AddEffect(GoapKey.shouldskin, false);

            outOfCombat = playerReader.PlayerBitValues.PlayerInCombat;
        }

        public override bool CheckIfActionCanRun()
        {
            return !bagReader.BagsFull && 
                playerReader.ShouldConsumeCorpse && 
                (
                bagReader.HasItem(7005) ||
                bagReader.HasItem(12709) ||
                bagReader.HasItem(19901)
                );
        }

        public override async Task PerformAction()
        {
            Log("Try to find Corpse");
            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Corpse);

            await stopMoving.Stop();

            // TODO: have to wait for the cursor to switch from loot -> skinning
            // sometimes takes a lot of time
            await npcNameFinder.WaitForNUpdate(1);
            if (!await Wait(500, DiDEnteredCombat()))
            {
                await AquireTarget();
                return;
            }

            Log("Should found corpses Count:" + npcNameFinder.NpcCount);
            WowPoint lastPosition = playerReader.PlayerLocation;
            bool skinSuccess = await npcNameFinder.FindByCursorType(Cursor.CursorClassification.Skin);
            if (skinSuccess)
            {
                await Wait(100);
                if (IsPlayerMoving(lastPosition)) 
                    Log("Goto corpse - Wait till the player become stil!");

                while (IsPlayerMoving(lastPosition))
                {
                    lastPosition = playerReader.PlayerLocation;
                    if (!await Wait(100, DiDEnteredCombat()))
                    {
                        await AquireTarget();
                        return;
                    }
                }
                
                await wowInput.TapInteractKey("Skinning Attempt...");
                do
                {
                    if (!await Wait(100, DiDEnteredCombat()))
                    {
                        await AquireTarget();
                        return;
                    }
                } while (playerReader.IsCasting);

                // Wait for to update the LastUIErrorMessage
                await Wait(100);

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

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                outOfCombat = true;
            }
        }

        async Task GoalExit()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, false));

            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Enemy);

            await wowInput.TapClearTarget();
        }

        public async Task<bool> DiDEnteredCombat()
        {
            await Task.Delay(0);
            if (!outOfCombat && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Combat Leave");
                outOfCombat = true;
            }

            if (outOfCombat && playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Combat detected");
                return true;
            }

            return false;
        }

        private async Task AquireTarget()
        {
            if (this.playerReader.PlayerBitValues.PlayerInCombat && this.playerReader.PetHasTarget)
            {
                await wowInput.TapTargetPet();
                Log($"Pets target {this.playerReader.TargetTarget}");
                if (this.playerReader.TargetTarget == TargetTargetEnum.PetHasATarget)
                {
                    Log("Found target by pet");
                    await wowInput.TapTargetOfTarget();
                    SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                    return;
                }

                await wowInput.TapNearestTarget();
                if (this.playerReader.HasTarget && playerReader.PlayerBitValues.TargetInCombat)
                {
                    if (this.playerReader.TargetTarget == TargetTargetEnum.TargetIsTargettingMe)
                    {
                        Log("Found from nearest target");
                        SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                        return;
                    }
                }

                await wowInput.TapClearTarget();
                Log("No target found");
            }
        }



        public override void ResetBeforePlanning()
        {
            base.ResetBeforePlanning();
        }

        private void Log(string text)
        {
            logger.LogInformation($"{this.GetType().Name}: {text}");
        }

        private bool IsPlayerMoving(WowPoint lastPos)
        {
            var distance = WowPoint.DistanceTo(lastPos, playerReader.PlayerLocation);
            return distance > 0.5f;
        }
    }
}
