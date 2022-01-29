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
        private readonly float RADIAN = MathF.PI * 2;

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

        private bool firstLoad = true;
        private bool shouldMount;
        private float lastDistance = float.MaxValue;
        private DateTime LastReset = DateTime.Now;

        private readonly int MinDistance;
        private readonly int MinDistanceMount = 15;
        private readonly int MaxDistance = 200;

        private readonly List<Vector3> pointsList;
        private readonly Stack<Vector3> routeToWaypoint = new();
        private readonly Stack<Vector3> wayPoints = new();

        private readonly Random random = new();

        #region IRouteProvider

        public DateTime LastActive { get; set; }

        public Stack<Vector3> PathingRoute()
        {
            return routeToWaypoint;
        }

        public bool HasNext()
        {
            return routeToWaypoint.Count != 0;
        }

        public Vector3 NextPoint()
        {
            return routeToWaypoint.Peek();
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
            this.stopMoving = stopMoving;

            this.pointsList = points;

            this.npcNameFinder = npcNameFinder;

            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            this.pather = pather;
            this.mountHandler = mountHandler;
            this.targetFinder = targetFinder;

            shouldMount = classConfiguration.UseMount;

            MinDistance = !(pather is RemotePathingAPIV3) ? MinDistanceMount : 10;

            if (classConfiguration.Mode != Mode.AttendedGather)
            {
                AddPrecondition(GoapKey.dangercombat, false);
                AddPrecondition(GoapKey.producedcorpse, false);
                AddPrecondition(GoapKey.consumecorpse, false);
            }
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this && e.Key != GoapKey.abort)
            {
                shouldMount = classConfiguration.UseMount;
                routeToWaypoint.Clear();
            }

            if (e.Key == GoapKey.abort)
            {
                targetFinderCts?.Cancel();
            }

            if (e.Key == GoapKey.resume)
            {
                if (classConfiguration.Mode != Mode.AttendedGather)
                {
                    StartLookingForTarget();
                }
            }
        }

        public override ValueTask OnEnter()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            if (classConfiguration.Mode != Mode.AttendedGather)
            {
                StartLookingForTarget();
            }

            return ValueTask.CompletedTask;
        }

        public override ValueTask OnExit()
        {
            targetFinderCts?.Cancel();

            return ValueTask.CompletedTask;
        }

        public override async ValueTask PerformAction()
        {
            if (playerReader.HasTarget && playerReader.Bits.TargetIsDead)
            {
                input.TapClearTarget("Target is dead.");
                wait.Update(1);
            }

            if (playerReader.Bits.IsDrowning)
            {
                StopDrowning();
            }

            if (classConfiguration.Mode == Mode.AttendedGather)
            {
                AlternateGatherTypes();
            }

            if (playerReader.Bits.PlayerInCombat && classConfiguration.Mode != Mode.AttendedGather) { return; }

            var timeSinceResetSeconds = (DateTime.Now - LastReset).TotalSeconds;
            if ((DateTime.Now - LastActive).TotalSeconds > 10 || routeToWaypoint.Count == 0 || timeSinceResetSeconds > 80)
            {
                Log("Trying to path a new route.");
                // recalculate next waypoint
                var pointsRemoved = 0;
                while (AdjustNextPointToClosest() && pointsRemoved < 5) { pointsRemoved++; };
                await RefillRouteToNextWaypoint(true);
                if (routeToWaypoint.Count == 0)
                {
                    logger.LogError("Didn't found path.");
                }
            }
            else
            {
                if (routeToWaypoint.Count > 0)
                {
                    var playerLocation = playerReader.PlayerLocation;
                    var distanceToRoute = playerLocation.DistanceXYTo(routeToWaypoint.Peek());
                    if (routeToWaypoint.Count < 1 && distanceToRoute > MaxDistance)
                    {
                        logger.LogError($"No route To Waypoint or too far {distanceToRoute} > {MaxDistance}");
                        routeToWaypoint.Pop();
                        return;
                    }
                }

                input.SetKeyState(input.ForwardKey, true, false);
            }

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(routeToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());

            if (distance < PointReachedDistance(MinDistance))
            {
                LogDebug("Move to next point");

                if (routeToWaypoint.Count > 0 && routeToWaypoint.Peek().Z != 0 && routeToWaypoint.Peek().Z != location.Z)
                {
                    playerReader.ZCoord = routeToWaypoint.Peek().Z;
                    LogDebug($"Update PlayerLocation.Z = {playerReader.ZCoord}");
                }

                if (routeToWaypoint.Count > 0)
                {
                    ReduceRouteByDistance(MinDistance);
                }

                //lastDistance = float.MaxValue;
                if (routeToWaypoint.Count == 0)
                {
                    wayPoints.Pop();
                    await RefillRouteToNextWaypoint(false);
                }

                this.stuckDetector.SetTargetLocation(routeToWaypoint.Peek());

                heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());
                AdjustHeading(heading, "Turn to next point");
            }
            else if (!stuckDetector.IsGettingCloser())
            {
                if (lastDistance < distance)
                {
                    AdjustNextPointToClosest();
                    AdjustHeading(heading, "Further away");
                }

                // stuck so jump
                input.SetKeyState(input.ForwardKey, true, false, "FollowRouteAction 2");
                wait.Update(1);

                if (HasBeenActiveRecently())
                {
                    await stuckDetector.Unstick();
                    distance = location.DistanceXYTo(routeToWaypoint.Peek());
                }
                else
                {
                    Log("Resume from stuck");
                }
            }
            else // distance closer
            {
                AdjustHeading(heading);
            }

            lastDistance = distance;

            MountIfRequired();

            RandomJump();

            LastActive = DateTime.Now;

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

        private void RefillWaypoints(bool findClosest = false)
        {
            LogDebug($"RefillWaypoints firstLoad:{firstLoad} - findClosest:{findClosest} - ThereAndBack:{classConfiguration.PathThereAndBack}");

            if (firstLoad)
            {
                firstLoad = false;
                var closestPoint = pointsList.OrderBy(p => playerReader.PlayerLocation.DistanceXYTo(p)).FirstOrDefault();

                for (int i = 0; i < pointsList.Count; i++)
                {
                    wayPoints.Push(pointsList[i]);
                    if (pointsList[i] == closestPoint) { break; }
                }
            }
            else
            {
                RefillWayPointsBasedonClassConfigRule();
                if (findClosest)
                {
                    AdjustNextPointToClosest();
                }
            }
        }

        private void RefillWayPointsBasedonClassConfigRule()
        {
            if (classConfiguration.PathThereAndBack)
            {
                var player = playerReader.PlayerLocation;
                var distanceToFirst = player.DistanceXYTo(pointsList[0]);
                var distanceToLast = player.DistanceXYTo(pointsList[^1]);

                if (distanceToLast > distanceToFirst)
                {
                    var reversed = pointsList.ToList();
                    reversed.Reverse();
                    reversed.ForEach(p => wayPoints.Push(p));
                }
                else
                {
                    pointsList.ForEach(p => wayPoints.Push(p));
                }
            }
            else
            {
                var reversed = pointsList.ToList();
                reversed.Reverse();
                reversed.ForEach(p => wayPoints.Push(p));
            }
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
            if (shouldMount && !mountHandler.IsMounted() && !playerReader.HasTarget)
            {
                if (classConfiguration.Mode != Mode.AttendedGather)
                {
                    shouldMount = false;
                }

                if (!npcNameFinder.MobsVisible)
                {
                    LogDebug("Mount up");
                    mountHandler.MountUp();
                    stuckDetector.ResetStuckParameters();
                }
                else
                {
                    LogDebug("Not mounting as can see NPC.");
                }

                input.SetKeyState(input.ForwardKey, true, false, "Move Forward");
            }
        }

        private void ReduceRouteByDistance(int minDistance)
        {
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(routeToWaypoint.Peek());
            while (distance < PointReachedDistance(minDistance - 1) && routeToWaypoint.Count > 0)
            {
                routeToWaypoint.Pop();
                if (routeToWaypoint.Count > 0)
                {
                    distance = location.DistanceXYTo(routeToWaypoint.Peek());
                }
            }
        }

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(routeToWaypoint.ToArray(), 0.05f);
            simple.Reverse();

            routeToWaypoint.Clear();
            simple.ForEach((x) => routeToWaypoint.Push(x));
        }

        private async ValueTask RefillRouteToNextWaypoint(bool forceUsePathing)
        {
            LastReset = DateTime.Now;

            if (wayPoints.Count == 0)
            {
                RefillWaypoints();
            }

            this.routeToWaypoint.Clear();

            var location = playerReader.PlayerLocation;
            var heading = DirectionCalculator.CalculateHeading(location, wayPoints.Peek());
            playerDirection.SetDirection(heading, wayPoints.Peek(), "Reached waypoint");

            //Create path back to route
            var distance = location.DistanceXYTo(wayPoints.Peek());
            if (forceUsePathing || distance > MaxDistance)
            {
                stopMoving.Stop();
                var path = await this.pather.FindRouteTo(addonReader, wayPoints.Peek());
                path.Reverse();
                path.ForEach(p => this.routeToWaypoint.Push(p));
            }

            //this.ReduceRouteByDistance();
            SimplyfyRouteToWaypoint();
            if (this.routeToWaypoint.Count == 0)
            {
                this.routeToWaypoint.Push(this.wayPoints.Peek());
            }

            this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());
        }

        private void AdjustHeading(float heading, string source = "")
        {
            var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var wanderAngle = 0.3;

            if (this.classConfiguration.Mode != Mode.AttendedGather)
            {
                wanderAngle = 0.05;
            }

            var diff = MathF.Min(diff1, diff2);
            if (diff > wanderAngle)
            {
                playerDirection.SetDirection(heading, routeToWaypoint.Peek(), source);
            }
        }

        private int PointReachedDistance(int distance)
        {
            return mountHandler.IsMounted() ? MinDistanceMount : distance;
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }

        private bool AdjustNextPointToClosest()
        {
            if (wayPoints.Count < 2) { return false; }

            var A = wayPoints.Pop();
            var B = wayPoints.Peek();
            var result = VectorExt.GetClosestPointOnLineSegment(A.AsVector2(), B.AsVector2(), playerReader.PlayerLocation.AsVector2());
            var newPoint = new Vector3(result.X, result.Y, 0);
            if (newPoint.DistanceXYTo(wayPoints.Peek()) > MinDistance)
            {
                wayPoints.Push(newPoint);
                LogDebug("Adjusted resume point");
                return false;
            }

            LogDebug("Skipped next point in path");
            return true;
        }

        private void RandomJump()
        {
            if (classConfiguration.Jump.MillisecondsSinceLastClick > random.Next(15_000, 45_000))
            {
                input.TapJump("Random jump");
            }
        }

        private void StopDrowning()
        {
            input.TapJump("Drowning! Swim up");
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