using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class FollowRouteGoal : GoapGoal
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly List<WowPoint> pointsList;
        private Stack<WowPoint> routeToWaypoint = new Stack<WowPoint>();
        private Stack<WowPoint> wayPoints = new Stack<WowPoint>();

        public List<WowPoint> RouteToWaypointList()
        {
            return routeToWaypoint.ToList();
        }

        public WowPoint? NextPoint()
        {
            return routeToWaypoint.Count == 0 ? null : routeToWaypoint.Peek();
        }

        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private readonly IPPather pather;
        private double lastDistance = 999;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);
        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        private DateTime lastTab = DateTime.Now;
        private readonly IBlacklist blacklist;
        private bool shouldMount = true;
        private ILogger logger;

        private bool firstLoad = true;

        public FollowRouteGoal(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, List<WowPoint> points, StopMoving stopMoving, NpcNameFinder npcNameFinder, IBlacklist blacklist, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            
            this.pointsList = points;

            this.npcNameFinder = npcNameFinder;
            this.blacklist = blacklist;
            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            this.pather = pather;

            if (classConfiguration.Mode != Mode.AttendedGather)
            {
                AddPrecondition(GoapKey.incombat, false);
            }
        }

        private void RefillWaypoints(bool findClosest = false)
        {
            if (firstLoad)
            {
                // start path at closest point
                firstLoad = false;
                var me = this.playerReader.PlayerLocation;
                var closest = pointsList.OrderBy(p => WowPoint.DistanceTo(me, p)).FirstOrDefault();

                for (int i = 0; i < pointsList.Count; i++)
                {
                    wayPoints.Push(pointsList[i]);
                    if (pointsList[i] == closest) { break; }
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

        public override float CostOfPerformingAction { get => 20f; }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                shouldMount = true;
                this.routeToWaypoint.Clear();
            }
        }

        private int lastGatherKey = 0;
        private DateTime lastGatherClick = DateTime.Now.AddSeconds(-10);

        public override async Task PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));
            if (this.classConfiguration.Mode == Mode.AttendedGather && this.lastGatherClick.AddSeconds(3)<DateTime.Now && this.classConfiguration.GatherFindKeyConfig.Count>0)
            {
                lastGatherKey++;
                if (lastGatherKey>= this.classConfiguration.GatherFindKeyConfig.Count)
                {
                    lastGatherKey = 0;
                }

                await wowProcess.KeyPress(classConfiguration.GatherFindKeyConfig[lastGatherKey].ConsoleKey, 200, "Gatherkey 1");
                lastGatherClick = DateTime.Now;
            }

            await Task.Delay(200);
            
            if (this.playerReader.PlayerBitValues.PlayerInCombat && this.classConfiguration.Mode != Mode.AttendedGather) { return; }

            if ((DateTime.Now - LastActive).TotalSeconds > 10 || routeToWaypoint.Count == 0)
            {
                // recalculate next waypoint
                var pointsRemoved = 0;
                while (AdjustNextPointToClosest() && pointsRemoved < 5) { pointsRemoved++; };
                await RefillRouteToNextWaypoint(true);
            }
            else
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 1");
            }

            await RandomJump();

            if (this.classConfiguration.Mode != Mode.AttendedGather)
            {
                // press tab
                if (!this.playerReader.PlayerBitValues.PlayerInCombat && (DateTime.Now - lastTab).TotalMilliseconds > 1100)
                {
                    //new PressKeyThread(this.wowProcess, ConsoleKey.Tab);
                    if (await LookForTarget())
                    {
                        if (this.playerReader.HasTarget)
                        {
                            logger.LogInformation("Has target!");
                            return;
                        }
                    }
                }
            }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());

            //if (this.classConfiguration.Mode == Mode.AttendedGather)
            //{
                await AdjustHeading(heading);
            //}

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 2");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await this.stuckDetector.Unstick();
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                await AdjustHeading(heading);
            }

            lastDistance = distance;

            if (distance < PointReachedDistance())
            {
                logger.LogInformation($"Move to next point");

                ReduceRoute();

                lastDistance = 999;
                if (routeToWaypoint.Count == 0)
                {
                    wayPoints.Pop();

                    await RefillRouteToNextWaypoint(false);
                }

                this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());

                heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Move to next point");

                distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());

                if (this.classConfiguration.Blink.ConsoleKey != 0 && this.playerReader.ManaPercentage > 90 && this.playerReader.PlayerLevel < 40
                    && distance> 200
                    )
                {
                    await wowProcess.KeyPress(this.classConfiguration.Blink.ConsoleKey, 120, this.classConfiguration.Blink.Name);
                }
            }

            // should mount
            await MountIfRequired();

            LastActive = DateTime.Now;
        }

        private async Task MountIfRequired()
        {
            if (shouldMount && !this.playerReader.PlayerBitValues.IsMounted && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                if (this.classConfiguration.Mode != Mode.AttendedGather)
                {
                    shouldMount = false;
                    //if (await LookForTarget()) { return; }
                }

                logger.LogInformation("Mounting if level >=40 (druid 30) and no NPC in sight");
                if (!this.npcNameFinder.MobsVisible)
                {
                    if (this.playerReader.PlayerLevel >= 40 && this.playerReader.PlayerClass != PlayerClassEnum.Druid)
                    {
                        await wowProcess.TapStopKey();
                        await Task.Delay(500);
                        await wowProcess.Mount(this.playerReader);
                    }
                    if (this.playerReader.PlayerLevel >= 30 && this.playerReader.PlayerClass == PlayerClassEnum.Druid)
                    {
                        this.classConfiguration.ShapeshiftForm
                          .Where(s => s.ShapeShiftFormEnum == ShapeshiftForm.Druid_Travel)
                          .ToList()
                          .ForEach(async k => await this.wowProcess.KeyPress(k.ConsoleKey, 325));
                    }
                }
                else
                {
                    logger.LogInformation("Not mounting as can see NPC.");
                }
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 3");
            }
        }

        private void ReduceRoute()
        {
            if (routeToWaypoint.Any())
            {
                var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
                var distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
                while (distance < PointReachedDistance() && routeToWaypoint.Any())
                {
                    routeToWaypoint.Pop();
                    if (routeToWaypoint.Any())
                    {
                        distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
                    }
                }
            }
        }

        private async Task RefillRouteToNextWaypoint(bool forceUsePathing)
        {
            if (wayPoints.Count == 0)
            {
                RefillWaypoints();
            }

            this.routeToWaypoint.Clear();

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var heading = DirectionCalculator.CalculateHeading(location, wayPoints.Peek());
            await playerDirection.SetDirection(heading, wayPoints.Peek(), "Reached waypoint").ConfigureAwait(false);

            //Create path back to route
            var distance = WowPoint.DistanceTo(location, wayPoints.Peek());
            if (forceUsePathing || distance > 200)
            {
                await this.stopMoving.Stop();
                var path = await this.pather.FindRouteTo(wayPoints.Peek());
                path.Reverse();
                path.ForEach(p => this.routeToWaypoint.Push(p));
            }

            this.ReduceRoute();
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

            if (Math.Min(diff1, diff2) > wanderAngle)
            {
                logger.LogInformation("Correct direction");
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Correcting direction");
            }
            else
            {
                logger.LogInformation($"Direction ok heading: {heading}, player direction {playerReader.Direction}");
            }
        }

        private int PointReachedDistance()
        {
            if (this.playerReader.PlayerClass == PlayerClassEnum.Druid && this.playerReader.Druid_ShapeshiftForm == ShapeshiftForm.Druid_Travel)
            {
                return 50;
            }

            return (this.playerReader.PlayerBitValues.IsMounted ? 50 : 40);
        }

        private async Task<bool> LookForTarget()
        {
            if (this.playerReader.HasTarget && !blacklist.IsTargetBlacklisted())
            {
                return true;
            }
            else
            {
                await this.wowProcess.KeyPress(ConsoleKey.Tab, 300);
                await Task.Delay(300);

                if (!playerReader.HasTarget)
                {
                    await this.npcNameFinder.FindAndClickNpc(0);
                    //await Task.Delay(300);
                }
            }

            if (this.playerReader.HasTarget && !blacklist.IsTargetBlacklisted())
            {
                if (playerReader.PlayerBitValues.IsMounted)
                {
                    await wowProcess.Dismount();
                }
                await this.TapInteractKey("FollowRouteAction 4");
                return true;
            }
            return false;
        }

        public async Task TapInteractKey(string source)
        {
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 99);
            this.classConfiguration.Interact.SetClicked();
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
            if ((DateTime.Now - LastJump).TotalSeconds > 10)
            {
                if (random.Next(1) == 0 && HasBeenActiveRecently())
                {
                    logger.LogInformation($"Random jump");

                    await wowProcess.KeyPress(ConsoleKey.Spacebar, 499);
                }
            }
            LastJump = DateTime.Now;
        }

        public async Task Reset()
        {
            await this.stopMoving.Stop();
        }

        public override async Task Abort()
        {
            await this.stopMoving.Stop();
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
    }
}