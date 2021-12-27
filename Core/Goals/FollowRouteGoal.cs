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

        private float RADIAN = MathF.PI * 2;

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

        private readonly bool debug = false;
        private bool firstLoad = true;
        private bool shouldMount = true;

        private readonly int MinDistance;
        private readonly int MinDistanceMount = 15;

        private float lastDistance = 999;

        private readonly List<Vector3> pointsList;
        private Stack<Vector3> routeToWaypoint = new Stack<Vector3>();
        private readonly Stack<Vector3> wayPoints = new Stack<Vector3>();

        private DateTime LastReset = DateTime.Now;

        private int lastGatherKey = 0;
        private DateTime lastGatherClick = DateTime.Now.AddSeconds(-10);

        private readonly Random random = new Random();


        #region IRouteProvider

        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        public List<Vector3> PathingRoute()
        {
            return routeToWaypoint.ToList();
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

        public override async ValueTask OnEnter()
        {
            await base.OnEnter();

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            if (classConfiguration.Mode != Mode.AttendedGather)
            {
                StartLookingForTarget();
            }
        }

        public override async ValueTask OnExit()
        {
            await base.OnExit();
            targetFinderCts?.Cancel();
        }

        public override async ValueTask PerformAction()
        {
            if (playerReader.HasTarget)
            {
                if (playerReader.Bits.TargetIsDead)
                {
                    await input.TapClearTarget("Target is dead.");
                    await wait.Update(1);
                    return;
                }

                await stopMoving.StopTurn();
                return;
            }

            if (playerReader.Bits.IsDrowning)
            {
                await StopDrowning();
            }

            await SwitchGatherType();

            if (this.playerReader.Bits.PlayerInCombat && classConfiguration.Mode != Mode.AttendedGather) { return; }

            var timeSinceResetSeconds = (DateTime.Now - LastReset).TotalSeconds;
            if ((DateTime.Now - LastActive).TotalSeconds > 10 || routeToWaypoint.Count == 0 || timeSinceResetSeconds > 80)
            {
                logger.LogInformation("Trying to path a new route.");
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
                var playerLocation = playerReader.PlayerLocation;
                if(routeToWaypoint.Count > 0)
                {
                    var distanceToRoute = playerLocation.DistanceXYTo(routeToWaypoint.Peek());
                    if (routeToWaypoint.Count < 1 && distanceToRoute > 200)
                    {
                        logger.LogError($"No route To Waypoint or too far {distanceToRoute}>200");
                        routeToWaypoint.Pop();
                        return;
                    }
                }

                input.SetKeyState(input.ForwardKey, true, false);
            }

            await RandomJump();

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(routeToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());

            await AdjustHeading(heading);

            if (lastDistance < distance)
            {
                AdjustNextPointToClosest();

                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                input.SetKeyState(input.ForwardKey, true, false, "FollowRouteAction 2");
                await wait.Update(1);
                if (HasBeenActiveRecently())
                {
                    await this.stuckDetector.Unstick();
                    distance = location.DistanceXYTo(routeToWaypoint.Peek());
                }
                else
                {
                    await wait.Update(1);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                await AdjustHeading(heading);
            }

            lastDistance = distance;

            if (distance < PointReachedDistance(MinDistance))
            {
                Log($"Move to next point");

                if (routeToWaypoint.Any())
                {
                    playerReader.ZCoord = routeToWaypoint.Peek().Z;
                    Log($"PlayerLocation.Z = {playerReader.PlayerLocation.Z}");
                }

                ReduceRouteByDistance(MinDistance);

                lastDistance = 999;
                if (routeToWaypoint.Count == 0)
                {
                    wayPoints.Pop();

                    await RefillRouteToNextWaypoint(false);
                }

                this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());

                heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Move to next point");
            }

            // should mount
            await MountIfRequired();

            LastActive = DateTime.Now;

            await wait.Update(1);
        }

        private void StartLookingForTarget()
        {
            targetFinderCts?.Dispose();
            targetFinderCts = new CancellationTokenSource();
            var task = Task.Factory.StartNew(async () =>
            {
                logger.LogInformation($"{GetType().Name}: .. Start searching for target...");

                bool found = false;
                while (!found && !targetFinderCts.IsCancellationRequested)
                {
                    found = await targetFinder.Search(GetType().Name, targetFinderCts.Token);
                    await wait.Update(1);
                }

                if (found)
                    logger.LogInformation($"{GetType().Name}: .. Found target!");

                if (targetFinderCts.IsCancellationRequested)
                    logger.LogInformation($"{GetType().Name}: .. Finding target aborted!");

            }, targetFinderCts.Token);
        }

        private void RefillWaypoints(bool findClosest = false)
        {
            if (firstLoad)
            {
                // start path at closest point
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
                if (findClosest)
                {
                    pointsList.ForEach(p => wayPoints.Push(p));
                    AdjustNextPointToClosest();
                }
                else
                {
                    pointsList.ForEach(p => wayPoints.Push(p));
                }
            }
        }

        private async ValueTask SwitchGatherType()
        {
            if (this.classConfiguration.Mode == Mode.AttendedGather && this.lastGatherClick.AddSeconds(3) < DateTime.Now && this.classConfiguration.GatherFindKeyConfig.Count > 0)
            {
                lastGatherKey++;
                if (lastGatherKey >= this.classConfiguration.GatherFindKeyConfig.Count)
                {
                    lastGatherKey = 0;
                }

                await input.KeyPress(classConfiguration.GatherFindKeyConfig[lastGatherKey].ConsoleKey, 200, "Gatherkey 1");
                lastGatherClick = DateTime.Now;
            }
        }

        private async ValueTask MountIfRequired()
        {
            if (shouldMount && !mountHandler.IsMounted() && !playerReader.Bits.PlayerInCombat)
            {
                if (classConfiguration.Mode != Mode.AttendedGather)
                {
                    shouldMount = false;
                    //if (await LookForTarget()) { return; }
                }

                Log("Mounting if level >=40 (druid 30) and no NPC in sight");
                if (!npcNameFinder.MobsVisible)
                {
                    await mountHandler.MountUp();
                    stuckDetector.ResetStuckParameters();
                }
                else
                {
                    logger.LogInformation("Not mounting as can see NPC.");
                }
                input.SetKeyState(input.ForwardKey, true, false, "Move Forward");
            }
        }

        private void ReduceRouteByDistance(int minDistance)
        {
            if (routeToWaypoint.Any())
            {
                var location = playerReader.PlayerLocation;
                var distance = location.DistanceXYTo(routeToWaypoint.Peek());
                while (distance < PointReachedDistance(minDistance - 1) && routeToWaypoint.Any())
                {
                    routeToWaypoint.Pop();
                    if (routeToWaypoint.Any())
                    {
                        distance = location.DistanceXYTo(routeToWaypoint.Peek());
                    }
                }
            }
        }

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(routeToWaypoint.ToArray(), 0.05f);
            simple.Reverse();
            routeToWaypoint = new Stack<Vector3>(simple);
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
            await playerDirection.SetDirection(heading, wayPoints.Peek(), "Reached waypoint");

            //Create path back to route
            var distance = location.DistanceXYTo(wayPoints.Peek());
            if (forceUsePathing || distance > 200)
            {
                await this.stopMoving.Stop();
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

        private async ValueTask AdjustHeading(float heading)
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
                /*
                if(diff > wanderAngle * 3)
                {
                    await stopMoving.StopForward();
                }
                */

                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Correcting direction");
            }
            else
            {
                //logger.LogInformation($"Direction ok heading: {heading}, player direction {playerReader.Direction}");
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
            if (newPoint.DistanceXYTo(wayPoints.Peek()) >= 4)
            {
                wayPoints.Push(newPoint);
                logger.LogInformation($"Adjusted resume point");
                return false;
            }
            else
            {
                logger.LogInformation($"Skipped next point in path");
                // skiped next point
                return true;
            }
        }

        private async ValueTask RandomJump()
        {
            if (classConfiguration.Jump.MillisecondsSinceLastClick > random.Next(10000, 15000))
            {
                await input.TapJump($"{GetType().Name}: Random jump");
            }
        }

        private async ValueTask StopDrowning()
        {
            await input.TapJump("Drowning! Swim up");
            await wait.Update(1);
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