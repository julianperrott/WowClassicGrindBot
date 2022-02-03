using Core.GOAP;
using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SharedLib.Extensions;

namespace Core.Goals
{
    public class FollowRouteGoal : GoapGoal, IRouteProvider
    {
        public override float CostOfPerformingAction { get => 20f; }

        private readonly bool debug = false;

        private readonly ILogger logger;
        private readonly ConfigurableInput input;
        private readonly Wait wait;
        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly NpcNameFinder npcNameFinder;
        private readonly ClassConfiguration classConfig;
        private readonly MountHandler mountHandler;
        private readonly Navigation navigation;
        private readonly List<Vector3> routePoints;

        private readonly TargetFinder targetFinder;
        private CancellationTokenSource? targetFinderCts;
        private Thread? targetFinderThread;
        private readonly int minMs = 500, maxMs = 1000;
        private readonly NpcNames NpcNameToFind = NpcNames.Enemy | NpcNames.Neutral;


        private bool shouldMount;

        private readonly Random random = new();

        #region IRouteProvider

        public DateTime LastActive => navigation.LastActive;

        public List<Vector3> PathingRoute()
        {
            return navigation.TotalRoute;
        }

        public bool HasNext()
        {
            return navigation.HasNext();
        }

        public Vector3 NextPoint()
        {
            return navigation.NextPoint();
        }

        #endregion


        public FollowRouteGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, ClassConfiguration classConfig, List<Vector3> points, Navigation navigation, MountHandler mountHandler, NpcNameFinder npcNameFinder, TargetFinder targetFinder)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.addonReader = addonReader;
            this.classConfig = classConfig;
            this.playerReader = addonReader.PlayerReader;
            this.routePoints = points;
            this.npcNameFinder = npcNameFinder;
            this.mountHandler = mountHandler;
            this.targetFinder = targetFinder;

            this.navigation = navigation;
            navigation.OnDestinationReached += Navigation_OnDestinationReached;
            navigation.OnWayPointReached += Navigation_OnWayPointReached;

            if (classConfig.Mode != Mode.AttendedGather)
            {
                AddPrecondition(GoapKey.dangercombat, false);
                AddPrecondition(GoapKey.producedcorpse, false);
                AddPrecondition(GoapKey.consumecorpse, false);
            }
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.abort)
            {
                targetFinderCts?.Cancel();
            }

            if (e.Key == GoapKey.resume)
            {
                if (classConfig.Mode != Mode.AttendedGather)
                {
                    StartLookingForTarget();
                    navigation.ResetStuckParameters();
                }
            }
        }

        public override ValueTask OnEnter()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            if (!navigation.HasWaypoint())
            {
                RefillWaypoints(true);
            }
            else
            {
                navigation.Resume();
            }

            if (classConfig.UseMount &&
                mountHandler.CanMount() && !shouldMount &&
                mountHandler.ShouldMount(navigation.TotalRoute.Last()))
            {
                shouldMount = true;
                Log("Mount up since desination far away");
            }

            if (classConfig.Mode != Mode.AttendedGather)
            {
                StartLookingForTarget();
            }

            return base.OnEnter();
        }

        public override ValueTask OnExit()
        {
            navigation.Stop();
            targetFinderCts?.Cancel();

            return base.OnExit();
        }

        public override async ValueTask PerformAction()
        {
            if (playerReader.HasTarget && playerReader.Bits.TargetIsDead)
            {
                input.TapClearTarget("Has target but its dead.");
                wait.Update(1);
            }

            if (playerReader.Bits.IsDrowning)
            {
                input.TapJump("Drowning! Swim up");
            }

            if (classConfig.Mode == Mode.AttendedGather)
            {
                AlternateGatherTypes();
            }

            if (playerReader.Bits.PlayerInCombat && classConfig.Mode != Mode.AttendedGather) { return; }

            await navigation.Update();

            RandomJump();

            wait.Update(1);
        }

        private void StartLookingForTarget()
        {
            targetFinderCts?.Dispose();
            targetFinderCts = new CancellationTokenSource();

            targetFinderThread = new Thread(Thread_LookingForTarget);
            targetFinderThread.Start();
        }

        private void Thread_LookingForTarget()
        {
            if (targetFinderCts == null)
            {
                logger.LogWarning($"{nameof(FollowRouteGoal)}: .. Unable to start search target!");
                return;
            }

            Log("Start searching for target...");

            Func<bool> validTarget = () =>
                playerReader.HasTarget &&
                !playerReader.Bits.TargetIsDead;

            bool found = false;
            while (!found && !playerReader.Bits.PlayerInCombat && !targetFinderCts.IsCancellationRequested)
            {
                if (classConfig.TargetNearestTarget.MillisecondsSinceLastClick > random.Next(minMs, maxMs) &&
                    !input.IsKeyDown(input.TurnLeftKey) && !input.IsKeyDown(input.TurnRightKey))
                {
                    found = targetFinder.Search(NpcNameToFind, validTarget, nameof(FollowRouteGoal), targetFinderCts.Token);
                }
                wait.Update(1);
            }

            if (found)
                Log("Found target!");

            if (targetFinderCts.IsCancellationRequested)
                Log("Finding target aborted!");
        }


        private void AlternateGatherTypes()
        {
            if (classConfig.GatherFindKeyConfig.Count < 1)
            {
                return;
            }

            var oldestKey = classConfig.GatherFindKeyConfig.OrderByDescending(x => x.MillisecondsSinceLastClick).First();
            if (oldestKey.MillisecondsSinceLastClick > 3000)
            {
                input.KeyPress(oldestKey.ConsoleKey, input.defaultKeyPress);
                oldestKey.SetClicked();
            }
        }

        private void MountIfRequired()
        {
            if (shouldMount && !mountHandler.IsMounted())
            {
                if (!npcNameFinder.MobsVisible)
                {
                    shouldMount = false;
                    Log("Mount up");
                    mountHandler.MountUp();
                    navigation.ResetStuckParameters();
                }
                else
                {
                    LogDebug("Not mounting as can see NPC.");
                }
            }
        }

        #region Refill rules

        private void Navigation_OnDestinationReached(object? sender, EventArgs e)
        {
            LogDebug("Navigation_OnDestinationReached");
            RefillWaypoints(false);
        }

        private void Navigation_OnWayPointReached(object? sender, EventArgs e)
        {
            MountIfRequired();
        }

        public void RefillWaypoints(bool onlyClosest)
        {
            Log($"RefillWaypoints - findClosest:{onlyClosest} - ThereAndBack:{input.ClassConfig.PathThereAndBack}");

            var player = playerReader.PlayerLocation;
            var path = routePoints.ToList();

            var distanceToFirst = player.DistanceXYTo(path[0]);
            var distanceToLast = player.DistanceXYTo(path[^1]);

            if (distanceToLast < distanceToFirst)
            {
                path.Reverse();
            }

            var closestPoint = path.ToList().OrderBy(p => player.DistanceXYTo(p)).First();
            if (onlyClosest)
            {
                var closestPath = new List<Vector3> { closestPoint };
                LogDebug($"RefillWaypoints: Closest wayPoint: {closestPoint}");
                navigation.SetWayPoints(closestPath);
                return;
            }

            int closestIndex = path.IndexOf(closestPoint);
            if (closestPoint == path[0] || closestPoint == path[^1])
            {
                if (input.ClassConfig.PathThereAndBack)
                {
                    navigation.SetWayPoints(path);
                }
                else
                {
                    path.Reverse();
                    navigation.SetWayPoints(path);
                }
            }
            else
            {
                var points = path.Take(closestIndex).ToList();
                points.Reverse();
                Log($"RefillWaypoints - Set destination from closest to nearest endpoint - with {points.Count} waypoints");
                navigation.SetWayPoints(points);
            }
        }

        #endregion

        private void RandomJump()
        {
            if (classConfig.Jump.MillisecondsSinceLastClick > random.Next(15_000, 45_000))
            {
                input.TapJump("Random jump");
            }
        }

        private void LogDebug(string text)
        {
            if (debug)
            {
                logger.LogDebug($"{nameof(FollowRouteGoal)}: {text}");
            }
        }

        private void Log(string text)
        {
            logger.LogInformation($"{nameof(FollowRouteGoal)}: {text}");
        }
    }
}