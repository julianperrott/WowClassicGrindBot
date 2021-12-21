using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class CombatGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4f; }

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly Wait wait;
        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly CastingHandler castingHandler;
        private readonly MountHandler mountHandler;

        private readonly ClassConfiguration classConfiguration;

        private float lastDirectionForTurnAround;

        private float lastKnwonPlayerDirection;
        private float lastKnownMinDistance;
        private float lastKnownMaxDistance;

        public CombatGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, StopMoving stopMoving, ClassConfiguration classConfiguration, CastingHandler castingHandler, MountHandler mountHandler)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;
            
            this.classConfiguration = classConfiguration;
            this.castingHandler = castingHandler;
            this.mountHandler = mountHandler;

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
            if (playerReader.Bits.HasPet && !playerReader.PetHasTarget)
            {
                await input.TapPetAttack("");
            }

            foreach (var item in Keys)
            {
                if (!playerReader.HasTarget)
                {
                    logger.LogInformation($"{GetType().Name}: Lost Target!");
                    await stopMoving.Stop();
                    return;
                }
                else
                {
                    lastKnwonPlayerDirection = playerReader.Direction;
                    lastKnownMinDistance = playerReader.MinRange;
                    lastKnownMaxDistance = playerReader.MaxRange;
                }

                if (await castingHandler.CastIfReady(item, item.DelayBeforeCast))
                {
                    if (item.Name == classConfiguration.Approach.Name ||
                        item.Name == classConfiguration.AutoAttack.Name)
                    {
                        await castingHandler.ReactToLastUIErrorMessage($"{GetType().Name}: Fight {item.Name}");
                    }

                    break;
                }
            }
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.newtarget)
            {
                logger.LogInformation($"{GetType().Name}: Reset cooldowns");

                ResetCooldowns();
            }

            if (e.Key == GoapKey.producedcorpse && (bool)e.Value)
            {
                // have to check range
                // ex. target died far away have to consider the range and approximate
                logger.LogInformation($"{GetType().Name}: --- Target is killed! Record death location.");
                float distance = (lastKnownMaxDistance + lastKnownMinDistance) / 2f;
                SendActionEvent(new ActionEventArgs(GoapKey.corpselocation, new CorpseLocation(GetCorpseLocation(distance), distance)));
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
                return this.playerReader.Bits.PlayerInCombat &&
                    !this.playerReader.Bits.TargetOfTargetIsPlayer
                    && this.playerReader.TargetHealthPercentage == 100;
            }
        }

        public override async ValueTask OnEnter()
        {
            await base.OnEnter();

            if (mountHandler.IsMounted())
            {
                await mountHandler.Dismount();
            }

            lastDirectionForTurnAround = playerReader.Direction;

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, true));
        }

        public override async ValueTask OnExit()
        {
            if (addonReader.CombatCreatureCount > 0)
            {
                await stopMoving.Stop();
            }
        }

        public override async ValueTask PerformAction()
        {
            if (MathF.Abs(lastDirectionForTurnAround - playerReader.Direction) > MathF.PI / 2)
            {
                logger.LogInformation($"{GetType().Name}: Turning too fast!");
                await stopMoving.Stop();

                lastDirectionForTurnAround = playerReader.Direction;
            }

            if (playerReader.Bits.IsDrowning)
            {
                await StopDrowning();
                return;
            }

            if (playerReader.HasTarget)
            {
                await Fight();
            }

            if (!playerReader.HasTarget && addonReader.CombatCreatureCount > 0)
            {
                await CreatureTargetMeOrMyPet();
            }

            await wait.Update(1);
        }

        private async Task CreatureTargetMeOrMyPet()
        {
            await wait.Update(1);
            if (playerReader.PetHasTarget && addonReader.CreatureHistory.CombatDeadGuid.Value != playerReader.PetTargetGuid)
            {
                logger.LogWarning("---- My pet has a target!");
                ResetCooldowns();

                await input.TapTargetPet();
                await input.TapTargetOfTarget();
                await wait.Update(1);
                return;
            }

            if (addonReader.CombatCreatureCount > 1)
            {
                await input.TapNearestTarget($"{GetType().Name}: Checking target in front of me");
                await wait.Update(1);
                if (playerReader.HasTarget)
                {
                    if (playerReader.Bits.TargetInCombat && playerReader.Bits.TargetOfTargetIsPlayer)
                    {
                        ResetCooldowns();

                        logger.LogWarning("---- Somebody is attacking me!");
                        await input.TapInteractKey("Found new target to attack");
                        await stopMoving.Stop();
                        await wait.Update(1);
                        return;
                    }

                    await input.TapClearTarget();
                    await wait.Update(1);
                }
                else
                {
                    // threat must be behind me
                    var anyDamageTakens = addonReader.CreatureHistory.DamageTaken.Where(x => (DateTime.Now - x.LastEvent).TotalSeconds < 10 && x.HealthPercent > 0);
                    if (anyDamageTakens.Any())
                    {
                        logger.LogWarning($"---- Possible threats found behind {anyDamageTakens.Count()}. Waiting for my target to change!");
                        await wait.Interrupt(2000, () => playerReader.HasTarget);
                    }
                }
            }
        }

        private async Task StopDrowning()
        {
            await input.TapJump("Drowning! Swim up");
            await wait.Update(1);
        }

        private Vector3 GetCorpseLocation(float distance)
        {
            return PointEsimator.GetPoint(playerReader.PlayerLocation, playerReader.Direction, distance);
        }
    }
}