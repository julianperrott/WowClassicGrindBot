using Core.Database;
using Core.GOAP;
using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using SharedLib.Extensions;

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
        private readonly IPlayerDirection playerDirection;

        private bool debug = true;
        private int lastLoot;

        private List<Vector3> corpseLocations = new();

        public LootGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameTargeting npcNameTargeting, CombatUtil combatUtil, IPlayerDirection playerDirection)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = addonReader.PlayerReader;
            this.areaDb = addonReader.AreaDb;
            this.stopMoving = stopMoving;
            this.bagReader = addonReader.BagReader;
            
            this.classConfiguration = classConfiguration;
            this.npcNameTargeting = npcNameTargeting;
            this.combatUtil = combatUtil;
            this.playerDirection = playerDirection;
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.dangercombat, false);
            AddPrecondition(GoapKey.shouldloot, true);
            AddEffect(GoapKey.shouldloot, false);
        }

        public override ValueTask OnEnter()
        {
            if (bagReader.BagsFull)
            {
                logger.LogWarning("Inventory is full");
                SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
            }

            Log($"OnEnter: Search for {NpcNames.Corpse}");
            npcNameTargeting.ChangeNpcType(NpcNames.Corpse);

            return ValueTask.CompletedTask;
        }

        public override ValueTask PerformAction()
        {
            lastLoot = playerReader.LastLootTime;

            stopMoving.Stop();
            combatUtil.Update();

            bool foundByCursor = false;

            npcNameTargeting.WaitForNUpdate(1);
            if (FoundByCursor())
            {
                foundByCursor = true;
                corpseLocations.Remove(GetClosestCorpse());
            }
            else if (corpseLocations.Count > 0)
            {
                var location = playerReader.PlayerLocation;
                var closestCorpse = GetClosestCorpse();
                var heading = DirectionCalculator.CalculateHeading(location, closestCorpse);
                playerDirection.SetDirection(heading, closestCorpse, "Look at possible corpse and try again");

                npcNameTargeting.WaitForNUpdate(1);
                if (FoundByCursor())
                {
                    foundByCursor = true;
                    corpseLocations.Remove(closestCorpse);
                }
            }

            if (!foundByCursor)
            {
                corpseLocations.Remove(GetClosestCorpse());

                input.TapLastTargetKey($"{nameof(LootGoal)}: No corpse name found - check last dead target exists");
                wait.Update(1);
                if (playerReader.HasTarget)
                {
                    if (playerReader.Bits.TargetIsDead)
                    {
                        CheckForSkinning();

                        input.TapInteractKey($"{nameof(LootGoal)}: Found last dead target");
                        wait.Update(1);

                        (bool foundTarget, bool moved) = combatUtil.FoundTargetWhileMoved();
                        if (foundTarget)
                        {
                            Log("Goal interrupted!");
                            return ValueTask.CompletedTask;
                        }

                        if (moved)
                        {
                            input.TapInteractKey($"{nameof(LootGoal)}: Last dead target double");
                        }
                    }
                    else
                    {
                        input.TapClearTarget($"{nameof(LootGoal)}: Don't attack the target!");
                    }
                }
            }

            GoalExit();
            return ValueTask.CompletedTask;
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.corpselocation && e.Value is CorpseLocation location)
            {
                //logger.LogInformation($"{nameof(LootGoal)}: --- Target is killed! Recorded death location.");
                corpseLocations.Add(location.WowPoint);
            }
        }

        private bool FoundByCursor()
        {
            if (!npcNameTargeting.FindBy(CursorType.Loot))
            {
                return false;
            }

            Log("Found corpse - clicked");
            (bool searchTimeOut, double elapsedMs) = wait.Until(200, () => playerReader.HasTarget);
            if (!searchTimeOut)
            {
                Log($"Found target after {elapsedMs}ms");
            }

            CheckForSkinning();

            (bool foundTarget, bool moved) = combatUtil.FoundTargetWhileMoved();
            if (foundTarget)
            {
                Log("Interrupted!");
                return false;
            }

            if (moved)
            {
                input.TapInteractKey($"{nameof(LootGoal)}: Had to move so interact again");
                wait.Update(1);
            }

            return true;
        }

        private Vector3 GetClosestCorpse()
        {
            if (corpseLocations.Count == 0)
                return Vector3.Zero;

            var closest = corpseLocations.
                Select(loc => new { loc, d = playerReader.PlayerLocation.DistanceXYTo(loc) }).
                Aggregate((a, b) => a.d <= b.d ? a : b);

            return closest.loc;
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

        private void GoalExit()
        {
            if (!wait.Till(1000, () => lastLoot != playerReader.LastLootTime))
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
                input.TapClearTarget($"{nameof(LootGoal)}: Exit Goal");
                wait.Update(1);
            }
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{nameof(LootGoal)}: {text}");
            }
        }
    }
}