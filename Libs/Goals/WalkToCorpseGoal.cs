using Libs.GOAP;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public partial class WalkToCorpseGoal : GoapGoal
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private double lastDistance = 999;
        private readonly List<WowPoint> spiritWalkerPath;
        private readonly List<WowPoint> routePoints;
        private readonly StuckDetector stuckDetector;
        private readonly IPPather pather;
        private Stack<WowPoint> points = new Stack<WowPoint>();

        public List<WowPoint> CorpseRunPathList()
        {
            return points.ToList();
        }

        public List<WowPoint> Deaths { get; } = new List<WowPoint>();

        private Random random = new Random();
        private ILogger logger;
        private DateTime LastJump = DateTime.Now;

        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        public WowPoint? NextPoint()
        {
            return points.Count == 0 ? null : points.Peek();
        }

        public WalkToCorpseGoal(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, List<WowPoint> spiritWalker, List<WowPoint> routePoints, StopMoving stopMoving, ILogger logger, StuckDetector stuckDetector, IPPather pather)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.routePoints = routePoints.ToList();
            this.spiritWalkerPath = spiritWalker.ToList();
            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.pather = pather;

            AddPrecondition(GoapKey.isdead, true);
        }

        public override float CostOfPerformingAction { get => 1f; }

        private WowPoint corpseLocation = new WowPoint(0, 0);

        private bool NeedsToReset = true;

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            NeedsToReset = true;
            points.Clear();
            this.corpseLocation = new WowPoint(0, 0);
        }

        public override async Task PerformAction()
        {
            if (NeedsToReset)
            {
                await this.stopMoving.Stop();

                while (true && this.playerReader.PlayerBitValues.DeadStatus)
                {
                    this.corpseLocation = new WowPoint(playerReader.CorpseX, playerReader.CorpseY);
                    if (this.corpseLocation.X > 0) { break; }
                    logger.LogInformation($"Waiting for corpse location to update {playerReader.CorpseX},{playerReader.CorpseY}");
                    await Task.Delay(1000);
                }
                logger.LogInformation($"Corpse location is {playerReader.CorpseX},{playerReader.CorpseY}");

                await Reset();
                

                Deaths.Add(this.corpseLocation);
            }

            await Task.Delay(200);

            if (!this.playerReader.PlayerBitValues.DeadStatus) { return; }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            double distance = 0;
            double heading = 0;

            if (points.Count == 0)
            {
                await Reset();
                if (!points.Any())
                {
                    points.Push(this.playerReader.CorpseLocation);
                    distance = DistanceTo(location, corpseLocation);
                    heading = DirectionCalculator.CalculateHeading(location, corpseLocation);
                    this.logger.LogInformation("no more points, heading to corpse");
                    await playerDirection.SetDirection(heading, this.playerReader.CorpseLocation, "Heading to corpse");
                    wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpse");
                    this.stuckDetector.SetTargetLocation(points.Peek());
                }
            }
            else
            {
                distance = DistanceTo(location, points.Peek());
                heading = DirectionCalculator.CalculateHeading(location, points.Peek());
            }

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, points.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpseAction");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await stuckDetector.Unstick();

                    await this.stopMoving.Stop();
                    await this.Reset();
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                var diff1 = Math.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
                var diff2 = Math.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

                if (Math.Min(diff1, diff2) > 0.3)
                {
                    await playerDirection.SetDirection(heading, points.Peek(), "Correcting direction");
                }
            }

            lastDistance = distance;

            if (distance < PointReachedDistance() && points.Any())
            {
                while (distance < PointReachedDistance() && points.Any())
                {
                    points.Pop();
                    if (points.Any())
                    {
                        distance = WowPoint.DistanceTo(location, points.Peek());
                    }
                }

                lastDistance = 999;
                if (points.Count > 0)
                {
                    heading = DirectionCalculator.CalculateHeading(location, points.Peek());
                    await playerDirection.SetDirection(heading, points.Peek(), "Move to next point");
                    this.stuckDetector.SetTargetLocation(points.Peek());
                }
            }

            LastActive = DateTime.Now;
        }

        private int PointReachedDistance()
        {
            return 40;
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }

        public async Task Reset()
        {
            await this.stopMoving.Stop();

            points.Clear();

            logger.LogInformation("Sleeping 5 seconds");
            await Task.Delay(5000);
            while (new List<double> { playerReader.XCoord, playerReader.YCoord, corpseLocation.X, corpseLocation.Y }.Max() > 100)
            {
                logger.LogInformation($"Waiting... odd coords read. Player {playerReader.XCoord},{playerReader.YCoord} corpse { corpseLocation.X}{corpseLocation.Y}");
                await Task.Delay(5000);
            }

            logger.LogInformation($"player location {playerReader.XCoord},{playerReader.YCoord}. Corpse {corpseLocation.X},{corpseLocation.Y}.");

            var path = await pather.FindRouteTo(corpseLocation);

            if (path.Any())
            {
                path.Reverse();
                path.ForEach(p => points.Push(p));
            }
            else
            {
                var closestRouteAndSpiritPathPoints = routePoints.SelectMany(s => spiritWalkerPath.Select(swp => (pathPoint: s, spiritPathPoint: swp, distance: DistanceTo(s, swp))))
                    .OrderBy(s => s.distance)
                    .First();

                // spirit walker path leg
                var spiritWalkerLeg = new List<WowPoint>();
                for (int i = 0; i < spiritWalkerPath.Count; i++)
                {
                    spiritWalkerLeg.Add(spiritWalkerPath[i]);
                    if (spiritWalkerPath[i] == closestRouteAndSpiritPathPoints.spiritPathPoint)
                    {
                        break;
                    }
                }

                var closestRoutePointToCorpse = routePoints.Select(s => (pathPoint: s, distance: DistanceTo(s, corpseLocation)))
                    .OrderBy(s => s.distance)
                    .First()
                    .pathPoint;

                //from closestRouteAndSpiritPathPoints to closestRoutePointToCorpse
                var pathStartPoint = closestRouteAndSpiritPathPoints.pathPoint;

                // see if we can walk forward through the points
                var legFromSpiritEndToCorpse = FillPathToCorpse(closestRoutePointToCorpse, pathStartPoint, routePoints);
                if (legFromSpiritEndToCorpse.Count == 0)
                {
                    var reversePath = routePoints.Select(s => s).ToList();
                    reversePath.Reverse();
                    legFromSpiritEndToCorpse = FillPathToCorpse(closestRoutePointToCorpse, pathStartPoint, reversePath);
                }

                var routeToCorpse = spiritWalkerLeg.Select(s => s).ToList();
                routeToCorpse.AddRange(legFromSpiritEndToCorpse);

                var myLocation = new WowPoint(playerReader.XCoord, playerReader.YCoord);
                var truncatedRoute = WowPoint.ShortenRouteFromLocation(myLocation, routeToCorpse);

                for (int i = truncatedRoute.Count - 1; i > -1; i--)
                {
                    points.Push(truncatedRoute[i]);
                }

                var cp = new CorpsePath { MyLocation = myLocation, CorpseLocation = corpseLocation };
                cp.RouteToCorpse.Clear();
                cp.RouteToCorpse.AddRange(routeToCorpse);
                cp.TruncatedRoute.Clear();
                cp.TruncatedRoute.AddRange(truncatedRoute);

#if DEBUG
                //File.WriteAllText($"CorpsePath_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json", JsonConvert.SerializeObject(cp));
#endif
            }
            if (points.Any())
            {
                lastDistance = 999;
                NeedsToReset = false;
                this.stuckDetector.SetTargetLocation(points.Peek());
                var heading = DirectionCalculator.CalculateHeading(this.playerReader.PlayerLocation, points.Peek());
                await playerDirection.SetDirection(heading, this.playerReader.CorpseLocation, "Heading to corpse");
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpse");
                this.stuckDetector.SetTargetLocation(points.Peek());
                this.LastActive = DateTime.Now;
            }
        }

        private static List<WowPoint> FillPathToCorpse(WowPoint closestRoutePointToCorpse, WowPoint pathStartPoint, List<WowPoint> routePoints)
        {
            var pathToCorpse = new List<WowPoint>();
            var startPathPointIndex = 0;
            var endPathPointIndex = 0;
            for (int i = 0; i < routePoints.Count; i++)
            {
                if (routePoints[i] == pathStartPoint)
                {
                    startPathPointIndex = i;
                }

                if (routePoints[i] == closestRoutePointToCorpse)
                {
                    endPathPointIndex = i;
                }
            }

            if (startPathPointIndex < endPathPointIndex)
            {
                for (int i = startPathPointIndex; i <= endPathPointIndex; i++)
                {
                    pathToCorpse.Add(routePoints[i]);
                }
            }

            return pathToCorpse;
        }

        public static bool IsDone()
        {
            return false;
        }

        private static double DistanceTo(WowPoint l1, WowPoint l2)
        {
            var x = l1.X - l2.X;
            var y = l1.Y - l2.Y;
            x = x * 100;
            y = y * 100;
            var distance = Math.Sqrt((x * x) + (y * y));

            //logger.LogInformation($"distance:{x} {y} {distance.ToString()}");
            return distance;
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

        private async Task RandomJump()
        {
            if ((DateTime.Now - LastJump).TotalSeconds > 5)
            {
                if (random.Next(1) == 0)
                {
                    logger.LogInformation($"Random jump");

                    await wowProcess.KeyPress(ConsoleKey.Spacebar, 499);
                }
            }
            LastJump = DateTime.Now;
        }
    }
}