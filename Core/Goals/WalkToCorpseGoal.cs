using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Goals
{
    public partial class WalkToCorpseGoal : GoapGoal, IRouteProvider
    {
        public override float CostOfPerformingAction { get => 1f; }

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private double lastDistance = 999;
        private readonly List<WowPoint> spiritWalkerPath;
        private readonly List<WowPoint> routePoints;
        private readonly StuckDetector stuckDetector;
        private readonly IPPather pather;

        private Stack<WowPoint> points = new Stack<WowPoint>();
        private double RADIAN = Math.PI * 2;

        public List<WowPoint> PathingRoute()
        {
            return points.ToList();
        }

        public List<WowPoint> Deaths { get; } = new List<WowPoint>();

        private Random random = new Random();

        private DateTime LastReset = DateTime.Now;
        private DateTime LastEventReceived = DateTime.Now;

        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        private WowPoint corpseLocation = new WowPoint(0, 0);

        private bool NeedsToReset = true;

        public WowPoint? NextPoint()
        {
            return points.Count == 0 ? null : points.Peek();
        }

        public WalkToCorpseGoal(ILogger logger, ConfigurableInput input, AddonReader addonReader, IPlayerDirection playerDirection, List<WowPoint> spiritWalker, List<WowPoint> routePoints, StopMoving stopMoving, StuckDetector stuckDetector, IPPather pather)
        {
            this.logger = logger;
            this.input = input;

            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.routePoints = routePoints.ToList();
            this.spiritWalkerPath = spiritWalker.ToList();
            
            this.stuckDetector = stuckDetector;
            this.pather = pather;

            AddPrecondition(GoapKey.isdead, true);
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            NeedsToReset = true;
            points.Clear();
            this.corpseLocation = new WowPoint(0, 0);
            LastEventReceived = DateTime.Now;
        }

        public override async Task OnEnter()
        {
            await base.OnEnter();
            playerReader.ZCoord = 0;
            logger.LogInformation($"{GetType().Name} Player got teleported to the graveyard!");

            addonReader.PlayerDied();
        }

        public override async Task PerformAction()
        {
            // is corpse visible
            if (this.playerReader.CorpseX < 1 && this.playerReader.CorpseX < 1)
            {
                await this.stopMoving.Stop();
                logger.LogInformation($"Waiting for corpse location to update update before performing action. Corpse is @ {playerReader.CorpseX},{playerReader.CorpseY}");
                await Task.Delay(5000);
                NeedsToReset = true;
                return;
            }

            if (NeedsToReset)
            {
                await this.stopMoving.Stop();

                while (this.playerReader.Bits.DeadStatus)
                {
                    this.corpseLocation = new WowPoint(playerReader.CorpseX, playerReader.CorpseY, playerReader.ZCoord);
                    if (this.corpseLocation.X >= 1 || this.corpseLocation.Y > 0) { break; }
                    logger.LogInformation($"Waiting for corpse location to update {playerReader.CorpseX},{playerReader.CorpseY}");
                    await Task.Delay(1000);
                }
                logger.LogInformation($"Corpse location is {playerReader.CorpseX},{playerReader.CorpseY}");

                await Reset();

                Deaths.Add(this.corpseLocation);
            }

            var timeSinceResetSeconds = (DateTime.Now - LastReset).TotalSeconds;
            if (timeSinceResetSeconds > 80)
            {
                await this.stopMoving.Stop();
                logger.LogInformation("We have been dead for over 1 minute, trying to path a new route.");
                await this.Reset();
            }

            await Task.Delay(200);

            if (!this.playerReader.Bits.DeadStatus) { return; }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord, playerReader.ZCoord);
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
                    input.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpse");
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
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpseAction");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await stuckDetector.Unstick();

                    // give up if we have been dead for 10 minutes
                    var timeDeadSeconds = (DateTime.Now - LastEventReceived).TotalSeconds;
                    if (timeDeadSeconds > 600)
                    {
                        logger.LogInformation("We have been dead for 10 minutes and seem to be stuck.");
                        SendActionEvent(new ActionEventArgs(GOAP.GoapKey.abort, true));
                        await Task.Delay(10000);
                        return;
                    }

                    distance = DistanceTo(location, points.Peek());
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
                if (points.Any())
                {
                    playerReader.ZCoord = points.Peek().Z;
                    logger.LogInformation($"{GetType().Name}: PlayerLocation.Z = {playerReader.PlayerLocation.Z}");
                }

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

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(points.ToArray(), 0.1f);
            simple.Reverse();
            points = new Stack<WowPoint>(simple);
        }

        public async Task Reset()
        {
            LastReset = DateTime.Now;

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

            var path = await pather.FindRouteTo(addonReader, corpseLocation);

            if (path.Any())
            {
                if (path.Any())
                {
                    playerReader.ZCoord = path[0].Z;
                    logger.LogInformation($"{GetType().Name}: PlayerLocation.Z = {playerReader.PlayerLocation.Z}");
                }

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

                var myLocation = new WowPoint(playerReader.XCoord, playerReader.YCoord, playerReader.ZCoord);
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
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpse");
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
            if (input.ClassConfig.Jump.MillisecondsSinceLastClick > random.Next(5000, 7000))
            {
                await input.TapJump($"{GetType().Name}: Random jump");
            }
        }
    }
}