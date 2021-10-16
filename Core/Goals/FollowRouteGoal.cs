using Core.GOAP;
using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class FollowRouteGoal : GoapGoal, IRouteProvider
    {
        public override float CostOfPerformingAction { get => 20f; }

        private double RADIAN = Math.PI * 2;

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly Wait wait;
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
        private double lastDistance = 999;

        private readonly List<WowPoint> pointsList;
        private Stack<WowPoint> routeToWaypoint = new Stack<WowPoint>();
        private readonly Stack<WowPoint> wayPoints = new Stack<WowPoint>();

        private DateTime LastReset = DateTime.Now;

        private int lastGatherKey = 0;
        private DateTime lastGatherClick = DateTime.Now.AddSeconds(-10);

        private readonly Random random = new Random();


        #region IRouteProvider

        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        public List<WowPoint> PathingRoute()
        {
            return routeToWaypoint.ToList();
        }

        public WowPoint? NextPoint()
        {
            return routeToWaypoint.Count == 0 ? null : routeToWaypoint.Peek();
        }

        #endregion


        public FollowRouteGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, IPlayerDirection playerDirection, List<WowPoint> points, StopMoving stopMoving, NpcNameFinder npcNameFinder, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather, MountHandler mountHandler, TargetFinder targetFinder)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;

            this.pointsList = points;

            this.npcNameFinder = npcNameFinder;

            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            this.pather = pather;
            this.mountHandler = mountHandler;
            this.targetFinder = targetFinder;

            MinDistance = !(pather is RemotePathingAPIV2) || !(pather is RemotePathingAPIV3) ? 15 : 8;

            if (classConfiguration.Mode != Mode.AttendedGather)
            {
                AddPrecondition(GoapKey.incombat, false);
                AddPrecondition(GoapKey.consumecorpse, false);
            }
        }

        public override bool CheckIfActionCanRun()
        {
            return !playerReader.ShouldConsumeCorpse && playerReader.LastCombatKillCount == 0;
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

        public override async Task OnEnter()
        {
            await base.OnEnter();

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            if (classConfiguration.Mode != Mode.AttendedGather)
            {
                StartLookingForTarget();
            }
        }

        public override async Task OnExit()
        {
            await base.OnExit();
            targetFinderCts?.Cancel();
        }

        public override async Task PerformAction()
        {
            if (playerReader.HasTarget)
            {
                if (playerReader.PlayerBitValues.TargetIsDead)
                {
                    await input.TapClearTarget("Target is dead.");
                    await wait.Update(1);
                    return;
                }

                await stopMoving.StopTurn();
                return;
            }

            if (playerReader.PlayerBitValues.IsDrowning)
            {
                await StopDrowning();
            }

            await SwitchGatherType();

            if (this.playerReader.PlayerBitValues.PlayerInCombat && classConfiguration.Mode != Mode.AttendedGather) { return; }

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
                var playerLocation = new WowPoint(playerReader.XCoord, playerReader.YCoord, playerReader.ZCoord);
                if(routeToWaypoint.Count > 0)
                {
                    var distanceToRoute = WowPoint.DistanceTo(playerLocation, routeToWaypoint.Peek());
                    if (routeToWaypoint.Count < 1 && distanceToRoute > 200)
                    {
                        logger.LogError($"No route To Waypoint or too far {distanceToRoute}>200");
                        routeToWaypoint.Pop();
                        return;
                    }
                }

                input.SetKeyState(ConsoleKey.UpArrow, true, false);
            }

            await RandomJump();

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord, playerReader.ZCoord);
            var distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
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
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 2");
                await wait.Update(1);
                if (HasBeenActiveRecently())
                {
                    await this.stuckDetector.Unstick();
                    distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
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

            await Task.Delay(10);
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
                var closestPoint = pointsList.OrderBy(p => WowPoint.DistanceTo(playerReader.PlayerLocation, p)).FirstOrDefault();

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

        private async Task SwitchGatherType()
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

        private async Task MountIfRequired()
        {
            if (shouldMount && !playerReader.PlayerBitValues.IsMounted && !playerReader.PlayerBitValues.PlayerInCombat)
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
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "Move Forward");
            }
        }

        private void ReduceRouteByDistance(int minDistance)
        {
            if (routeToWaypoint.Any())
            {
                var location = new WowPoint(playerReader.XCoord, playerReader.YCoord, playerReader.ZCoord);
                var distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
                while (distance < PointReachedDistance(minDistance - 1) && routeToWaypoint.Any())
                {
                    routeToWaypoint.Pop();
                    if (routeToWaypoint.Any())
                    {
                        distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
                    }
                }
            }
        }

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(routeToWaypoint.ToArray(), 0.05f);
            simple.Reverse();
            routeToWaypoint = new Stack<WowPoint>(simple);
        }

        private async Task RefillRouteToNextWaypoint(bool forceUsePathing)
        {
            LastReset = DateTime.Now;

            if (wayPoints.Count == 0)
            {
                RefillWaypoints();
            }

            this.routeToWaypoint.Clear();

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord, playerReader.ZCoord);
            var heading = DirectionCalculator.CalculateHeading(location, wayPoints.Peek());
            await playerDirection.SetDirection(heading, wayPoints.Peek(), "Reached waypoint").ConfigureAwait(false);

            //Create path back to route
            var distance = WowPoint.DistanceTo(location, wayPoints.Peek());
            if (forceUsePathing || distance > 200)
            {
                await this.stopMoving.Stop();
                var path = await this.pather.FindRouteTo(this.playerReader, wayPoints.Peek());
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

        private async Task AdjustHeading(double heading)
        {
            var diff1 = Math.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = Math.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var wanderAngle = 0.3;

            if (this.classConfiguration.Mode != Mode.AttendedGather)
            {
                wanderAngle = 0.05;
            }

            var diff = Math.Min(diff1, diff2);
            if (diff > wanderAngle)
            {
                if(diff > wanderAngle * 3)
                {
                    await stopMoving.StopForward();
                }

                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Correcting direction");
            }
            else
            {
                //logger.LogInformation($"Direction ok heading: {heading}, player direction {playerReader.Direction}");
            }
        }

        private int PointReachedDistance(int distance)
        {
            if (this.playerReader.PlayerClass == PlayerClassEnum.Druid && this.playerReader.Form == Form.Druid_Travel)
            {
                return 50;
            }

            return (this.playerReader.PlayerBitValues.IsMounted ? 50 : distance);
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
            var result = GetClosestPointOnLineSegment(A.Vector2(), B.Vector2(), new Vector2((float)this.playerReader.XCoord, (float)this.playerReader.YCoord));
            var newPoint = new WowPoint(result.X, result.Y);
            if (WowPoint.DistanceTo(newPoint, wayPoints.Peek()) >= 4)
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

        private async Task RandomJump()
        {
            if (classConfiguration.Jump.MillisecondsSinceLastClick > random.Next(10000, 15000))
            {
                await input.TapJump($"{GetType().Name}: Random jump");
            }
        }

        public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 AP = P - A;       //Vector from A to P
            Vector2 AB = B - A;       //Vector from A to B

            float magnitudeAB = AB.LengthSquared();     //Magnitude of AB vector (it's length squared)
            float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point

            if (distance < 0)     //Check if P projection is over vectorAB
            {
                return A;
            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB * distance;
            }
        }


        private async Task StopDrowning()
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