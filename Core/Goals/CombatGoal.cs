using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class CombatGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4f; }

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly Wait wait;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly CastingHandler castingHandler;
        
        private readonly ClassConfiguration classConfiguration;

        private int lastKilledGuid;

        public CombatGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, StopMoving stopMoving,  ClassConfiguration classConfiguration, CastingHandler castingHandler)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            
            this.classConfiguration = classConfiguration;
            this.castingHandler = castingHandler;

            lastKilledGuid = playerReader.LastKilledGuid;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);
            AddPrecondition(GoapKey.incombatrange, true);

            AddEffect(GoapKey.producedcorpse, true);
            AddEffect(GoapKey.targetisalive, false);
            AddEffect(GoapKey.hastarget, false);

            classConfiguration.Combat.Sequence.Where(k => k != null).ToList().ForEach(key => Keys.Add(key));
        }

        protected async Task Fight()
        {
            if (playerReader.PlayerBitValues.HasPet && !playerReader.PetHasTarget)
            {
                await input.TapPetAttack("");
            }

            foreach (var item in Keys)
            {
                if (!playerReader.HasTarget)
                {
                    logger.LogInformation($"{GetType().Name}: Lost Target!");
                    await stopMoving.Stop();
                    break;
                }

                if (playerReader.IsAutoAttacking)
                {
                    await castingHandler.ReactToLastUIErrorMessage($"{GetType().Name}: Fight AutoAttacking");
                }

                if (await castingHandler.CastIfReady(item, item.DelayBeforeCast))
                {
                    break;
                }
            }
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.newtarget)
            {
                logger.LogInformation("?Reset cooldowns");

                ResetCooldowns();
            }
        }

        private void ResetCooldowns()
        {
            this.classConfiguration.Combat.Sequence
            .Where(i => i.ResetOnNewTarget)
            .ToList()
            .ForEach(item =>
            {
                logger.LogInformation($"Reset cooldown on {item.Name}");
                item.ResetCooldown();
                item.ResetCharges();
            });
        }

        protected bool HasPickedUpAnAdd
        {
            get
            {
                return this.playerReader.PlayerBitValues.PlayerInCombat &&
                    !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer
                    && this.playerReader.TargetHealthPercentage == 100;
            }
        }

        public override async Task OnEnter()
        {
            await base.OnEnter();

            if (playerReader.PlayerBitValues.IsMounted)
            {
                await input.TapDismount();
            }

            await stopMoving.Stop();

            logger.LogInformation($"{GetType().Name}: OnEnter");
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, true));
        }

        public override async Task PerformAction()
        {
            bool hasTarget = playerReader.HasTarget;

            await Fight();
            await KillCheck(hasTarget);

            await Task.Delay((int)(playerReader.AvgUpdateLatency / 2));
        }

        private async Task KillCheck(bool hasTarget)
        {
            await wait.Update(1);
            if (hasTarget != playerReader.HasTarget)
            {
                (bool lastkilledGuidNotChanged, double elapsedMs) = await wait.InterruptTask(300, 
                    () => lastKilledGuid != playerReader.LastKilledGuid);
                if (!lastkilledGuidNotChanged)
                {
                    logger.LogInformation($"Target Death detected after {elapsedMs}ms");
                }
            }

            if (DidKilledACreature())
            {
                if (!await CreatureTargetMeOrMyPet())
                {
                    logger.LogInformation("Exit CombatGoal!!!");
                }
            }
        }

        private bool DidKilledACreature()
        {
            if (lastKilledGuid != playerReader.LastKilledGuid)
            {
                //logger.LogInformation($"----- A mob just died {playerReader.LastKilledGuid}");

                if ((playerReader.CombatCreatures.Any(x => x.CreatureId == playerReader.LastKilledGuid) || // creature dealt damage to me or my pet
                playerReader.TargetHistory.Any(x => x.CreatureId == playerReader.LastKilledGuid)))     // has ever targeted by the player)
                {
                    lastKilledGuid = playerReader.LastKilledGuid;

                    playerReader.IncrementKillCount();
                    logger.LogInformation($"----- Killed a mob! Current: {playerReader.LastCombatKillCount} - " + 
                        $"CombatCreature: {playerReader.CombatCreatures.Any(x => x.CreatureId == playerReader.LastKilledGuid)} - " + 
                        $"TargetHistory: {playerReader.TargetHistory.Any(x => x.CreatureId == playerReader.LastKilledGuid)}");

                    SendActionEvent(new ActionEventArgs(GoapKey.producedcorpse, true));
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> CreatureTargetMeOrMyPet()
        {
            if (playerReader.PetHasTarget &&
                playerReader.LastKilledGuid != playerReader.PetTargetGuid)
            {
                logger.LogWarning("---- My pet has a target!");
                ResetCooldowns();

                await input.TapTargetPet();
                await input.TapTargetOfTarget();
                await wait.Update(1);
                return playerReader.HasTarget;
            }

            // check for targets attacking me
            await input.TapNearestTarget();
            await wait.Update(1);
            if (playerReader.HasTarget)
            {
                if (playerReader.PlayerBitValues.TargetInCombat && playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                {
                    ResetCooldowns();

                    logger.LogWarning("---- Somebody is attacking me!");
                    await input.TapInteractKey("Found new target to attack");
                    await wait.Update(1);
                    return true;
                }

                await input.TapClearTarget();
                await wait.Update(1);
            }

            logger.LogWarning("---- No Threat has been found!");

            return false;
        }
    }
}