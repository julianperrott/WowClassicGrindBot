using Core.GOAP;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
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
        private float lastDistance = 999;
        private readonly List<Vector3> spiritWalkerPath;
        private readonly List<Vector3> routePoints;
        private readonly StuckDetector stuckDetector;
        private readonly IPPather pather;

        private Stack<Vector3> points = new Stack<Vector3>();
        private float RADIAN = MathF.PI * 2;

        public List<Vector3> PathingRoute()
        {
            return points.ToList();
        }

        public List<Vector3> Deaths { get; } = new List<Vector3>();

        private Random random = new Random();

        private DateTime LastReset = DateTime.Now;
        private DateTime LastEventReceived = DateTime.Now;

        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        private Vector3 corpseLocation;

        private bool NeedsToReset = true;

        public bool HasNext()
        {
            return points.Count != 0;
        }

        public Vector3 NextPoint()
        {
            return points.Peek();
        }

        public WalkToCorpseGoal(ILogger logger, ConfigurableInput input, AddonReader addonReader, IPlayerDirection playerDirection, List<Vector3> spiritWalker, List<Vector3> routePoints, StopMoving stopMoving, StuckDetector stuckDetector, IPPather pather)
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
            this.corpseLocation = Vector3.Zero;
            LastEventReceived = DateTime.Now;
        }

        public override ValueTask OnEnter()
        {
            playerReader.ZCoord = 0;
            logger.LogInformation($"{GetType().Name} Player got teleported to the graveyard!");

            addonReader.PlayerDied();

            return ValueTask.CompletedTask;
        }

        public override async ValueTask PerformAction()
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
                    this.corpseLocation = playerReader.CorpseLocation;
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

            var location = playerReader.PlayerLocation;
            float distance = 0;
            float heading = 0;

            if (points.Count == 0)
            {
                await Reset();
                if (!points.Any())
                {
                    points.Push(this.playerReader.CorpseLocation);
                    distance = location.DistanceXYTo(corpseLocation);
                    heading = DirectionCalculator.CalculateHeading(location, corpseLocation);
                    this.logger.LogInformation("no more points, heading to corpse");
                    await playerDirection.SetDirection(heading, this.playerReader.CorpseLocation, "Heading to corpse");
                    input.SetKeyState(input.ForwardKey, true, false, "WalkToCorpse");
                    this.stuckDetector.SetTargetLocation(points.Peek());
                }
            }
            else
            {
                distance = location.DistanceXYTo(points.Peek());
                heading = DirectionCalculator.CalculateHeading(location, points.Peek());
            }

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, points.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                input.SetKeyState(input.ForwardKey, true, false, "WalkToCorpseAction");
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

                    distance = location.DistanceXYTo(points.Peek());
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
                var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

                if (MathF.Min(diff1, diff2) > 0.3)
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
                        distance = location.DistanceXYTo(points.Peek());
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

        private static int PointReachedDistance()
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
            points = new Stack<Vector3>(simple);
        }

        public async ValueTask Reset()
        {
            LastReset = DateTime.Now;

            await this.stopMoving.Stop();

            points.Clear();

            logger.LogInformation("Sleeping 5 seconds");
            await Task.Delay(5000);
            while (new List<float> { playerReader.XCoord, playerReader.YCoord, corpseLocation.X, corpseLocation.Y }.Max() > 100)
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
                var closestRouteAndSpiritPathPoints = routePoints.SelectMany(s => spiritWalkerPath.Select(swp => (pathPoint: s, spiritPathPoint: swp, distance: s.DistanceXYTo(swp))))
                    .OrderBy(s => s.distance)
                    .First();

                // spirit walker path leg
                var spiritWalkerLeg = new List<Vector3>();
                for (int i = 0; i < spiritWalkerPath.Count; i++)
                {
                    spiritWalkerLeg.Add(spiritWalkerPath[i]);
                    if (spiritWalkerPath[i] == closestRouteAndSpiritPathPoints.spiritPathPoint)
                    {
                        break;
                    }
                }

                var closestRoutePointToCorpse = routePoints.Select(s => (pathPoint: s, distance: corpseLocation.DistanceXYTo(s)))
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

                var myLocation = playerReader.PlayerLocation;
                var truncatedRoute = VectorExt.ShortenRouteFromLocation(myLocation, routeToCorpse);

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
                input.SetKeyState(input.ForwardKey, true, false, "WalkToCorpse");
                this.stuckDetector.SetTargetLocation(points.Peek());
                this.LastActive = DateTime.Now;
            }
        }

        private static List<Vector3> FillPathToCorpse(Vector3 closestRoutePointToCorpse, Vector3 pathStartPoint, List<Vector3> routePoints)
        {
            var pathToCorpse = new List<Vector3>();
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

        private async ValueTask RandomJump()
        {
            if (input.ClassConfig.Jump.MillisecondsSinceLastClick > random.Next(5000, 7000))
            {
                await input.TapJump($"{GetType().Name}: Random jump");
            }
        }
    }
}