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
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private readonly IPPather pather;
        private readonly MountHandler mountHandler;
        private readonly TargetFinder targetFinder;
        private CancellationTokenSource? targetFinderCts;
        private Thread? targetFinderThread;

        private readonly List<Vector3> routePoints;

        private bool shouldMount;

        private readonly Random random = new();

        private readonly Navigation navigation;

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


        public FollowRouteGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, IPlayerDirection playerDirection, List<Vector3> points, StopMoving stopMoving, NpcNameFinder npcNameFinder, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather, MountHandler mountHandler, TargetFinder targetFinder)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.playerDirection = playerDirection;
            this.routePoints = points;
            this.stopMoving = stopMoving;
            this.npcNameFinder = npcNameFinder;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            this.pather = pather;
            this.mountHandler = mountHandler;
            this.targetFinder = targetFinder;

            navigation = new Navigation(logger, playerDirection, input, addonReader, wait, stopMoving, stuckDetector, pather, mountHandler);
            navigation.OnDestinationReached += Navigation_OnDestinationReached;

            if (classConfiguration.Mode != Mode.AttendedGather)
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
                if (classConfiguration.Mode != Mode.AttendedGather)
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

            if (classConfiguration.UseMount &&
                mountHandler.CanMount() && !shouldMount &&
                mountHandler.ShouldMount(navigation.TotalRoute.Last()))
            {
                shouldMount = true;
                Log("Mount up since desination far away");
            }

            if (classConfiguration.Mode != Mode.AttendedGather)
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

            if (classConfiguration.Mode == Mode.AttendedGather)
            {
                AlternateGatherTypes();
            }

            if (playerReader.Bits.PlayerInCombat && classConfiguration.Mode != Mode.AttendedGather) { return; }

            await navigation.Update();

            MountIfRequired();

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

            bool found = false;
            while (!found && !targetFinderCts.IsCancellationRequested)
            {
                found = targetFinder.Search(nameof(FollowRouteGoal), targetFinderCts.Token);
                wait.Update(1);
            }

            if (found)
                Log("Found target!");

            if (targetFinderCts.IsCancellationRequested)
                Log("Finding target aborted!");
        }


        private void AlternateGatherTypes()
        {
            if (classConfiguration.GatherFindKeyConfig.Count < 1)
            {
                return;
            }

            var oldestKey = classConfiguration.GatherFindKeyConfig.OrderByDescending(x => x.MillisecondsSinceLastClick).First();
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
                    stuckDetector.ResetStuckParameters();
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
            if (classConfiguration.Jump.MillisecondsSinceLastClick > random.Next(15_000, 45_000))
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