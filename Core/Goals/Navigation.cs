using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;

namespace Core.Goals
{
    public class Navigation
    {
        private readonly bool debug = false;
        private readonly float RADIAN = MathF.PI * 2;

        private readonly ILogger logger;
        private readonly IPlayerDirection playerDirection;
        private readonly ConfigurableInput input;
        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly Wait wait;
        private readonly StopMoving stopMoving;
        private readonly StuckDetector stuckDetector;
        private readonly IPPather pather;
        private readonly MountHandler mountHandler;

        private readonly int MinDistance = 10;
        private readonly int MinDistanceMount = 15;
        private readonly int MaxDistance = 200;

        private bool firstLoad = true;
        private bool useClassConfigRule;
        private float lastDistance = float.MaxValue;

        private readonly List<Vector3> routePoints;
        private readonly Stack<Vector3> wayPoints = new();
        public Stack<Vector3> RouteToWaypoint { private init; get; } = new();

        public List<Vector3> TotalRoute { private init; get; } = new();

        public DateTime LastActive { get; set; }

        public event EventHandler? OnWayPointReached;
        public event EventHandler? OnDestinationReached;

        public bool PreciseMovement { get; set; }
        public bool AllowReduceByDistance { get; set; } = true;

        public Navigation(ILogger logger, IPlayerDirection playerDirection, ConfigurableInput input, AddonReader addonReader, Wait wait, StopMoving stopMoving, StuckDetector stuckDetector, IPPather pather, MountHandler mountHandler, List<Vector3> points, bool useClassConfigRule)
        {
            this.logger = logger;
            this.playerDirection = playerDirection;
            this.input = input;
            this.addonReader = addonReader;
            playerReader = addonReader.PlayerReader;
            this.wait = wait;
            this.stopMoving = stopMoving;
            this.stuckDetector = stuckDetector;
            this.pather = pather;
            this.mountHandler = mountHandler;

            routePoints = points;
            this.useClassConfigRule = useClassConfigRule;

            MinDistance = pather is not RemotePathingAPIV3 ? MinDistanceMount : 10;
        }

        public async ValueTask Update()
        {
            if (RouteToWaypoint.Count == 0)
            {
                Log("Trying to path a new route.");

                // recalculate next waypoint
                var pointsRemoved = 0;
                while (AdjustNextPointToClosest() && pointsRemoved < 5) { pointsRemoved++; };

                await RefillRouteToNextWaypoint(true);

                if (RouteToWaypoint.Count == 0)
                {
                    logger.LogError("No path found!");
                }
            }
            else
            {
                var playerLocation = playerReader.PlayerLocation;
                var distanceToRoute = playerLocation.DistanceXYTo(RouteToWaypoint.Peek());
                if (RouteToWaypoint.Count < 1 && distanceToRoute > MaxDistance)
                {
                    logger.LogError($"No route To Waypoint or too far {distanceToRoute} > {MaxDistance}");
                    RouteToWaypoint.Pop();
                    return;
                }

                input.SetKeyState(input.ForwardKey, true, false);
            }


            // main loop

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(RouteToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, RouteToWaypoint.Peek());

            if (distance < PointReachedDistance(MinDistance))
            {
                LogDebug("Move to next point");

                if (RouteToWaypoint.Count > 0 && RouteToWaypoint.Peek().Z != 0 && RouteToWaypoint.Peek().Z != location.Z)
                {
                    playerReader.ZCoord = RouteToWaypoint.Peek().Z;
                    LogDebug($"Update PlayerLocation.Z = {playerReader.ZCoord}");
                }

                OnWayPointReached?.Invoke(this, EventArgs.Empty);

                if (RouteToWaypoint.Count > 0)
                {
                    if (AllowReduceByDistance)
                        ReduceRouteByDistance(MinDistance);
                    else
                        RouteToWaypoint.Pop();
                }

                if (RouteToWaypoint.Count == 0)
                {
                    if (wayPoints.Count > 0)
                    {
                        wayPoints.Pop();
                    }

                    OnDestinationReached?.Invoke(this, EventArgs.Empty);
                    return;
                }
                else
                {
                    stuckDetector.SetTargetLocation(RouteToWaypoint.Peek());

                    heading = DirectionCalculator.CalculateHeading(location, RouteToWaypoint.Peek());
                    AdjustHeading(heading, "Turn to next point");
                }
            }
            else if (RouteToWaypoint.Count > 0)
            {
                if (!stuckDetector.IsGettingCloser())
                {
                    if (lastDistance < distance)
                    {
                        if (!PreciseMovement)
                        {
                            AdjustNextPointToClosest();
                        }

                        AdjustHeading(heading, "Further away");
                    }

                    // stuck so jump
                    input.SetKeyState(input.ForwardKey, true, false, "FollowRouteAction 2");
                    wait.Update(1);

                    if (HasBeenActiveRecently())
                    {
                        await stuckDetector.Unstick();
                        distance = location.DistanceXYTo(RouteToWaypoint.Peek());
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
            }

            lastDistance = distance;

            LastActive = DateTime.Now;
        }

        public void UpdatedRouteToWayPoint()
        {
            wayPoints.Clear();
            stuckDetector.SetTargetLocation(RouteToWaypoint.Peek());
        }

        public void LookAtNextWayPoint()
        {
            var location = playerReader.PlayerLocation;
            var heading = DirectionCalculator.CalculateHeading(location, RouteToWaypoint.Peek());
            AdjustHeading(heading, "LookAtNextWayPoint");
        }



        private int PointReachedDistance(int distance)
        {
            return mountHandler.IsMounted() ? MinDistanceMount : distance;
        }

        private void ReduceRouteByDistance(int minDistance)
        {
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(RouteToWaypoint.Peek());
            while (distance < PointReachedDistance(minDistance - 1) && RouteToWaypoint.Count > 0)
            {
                RouteToWaypoint.Pop();
                if (RouteToWaypoint.Count > 0)
                {
                    distance = location.DistanceXYTo(RouteToWaypoint.Peek());
                }
            }
        }

        private void AdjustHeading(float heading, string source = "")
        {
            var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var wanderAngle = 0.3;
            if (input.ClassConfig.Mode != Mode.AttendedGather)
            {
                wanderAngle = 0.05;
            }

            var diff = MathF.Min(diff1, diff2);
            if (diff > wanderAngle)
            {
                playerDirection.SetDirection(heading, RouteToWaypoint.Peek(), source, PreciseMovement ? 0 : MinDistance);
            }
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

        public async ValueTask RefillRouteToNextWaypoint(bool forceUsePathing)
        {
            LastReset = DateTime.Now;

            if (wayPoints.Count == 0)
            {
                RefillWaypoints();
            }

            RouteToWaypoint.Clear();

            var location = playerReader.PlayerLocation;
            var heading = DirectionCalculator.CalculateHeading(location, wayPoints.Peek());
            playerDirection.SetDirection(heading, wayPoints.Peek(), "Reached waypoint");

            //Create path back to route
            var distance = location.DistanceXYTo(wayPoints.Peek());
            if (forceUsePathing || distance > MaxDistance)
            {
                stopMoving.Stop();
                var path = await pather.FindRouteTo(addonReader, wayPoints.Peek());
                path.Reverse();
                path.ForEach(p => RouteToWaypoint.Push(p));
            }

            SimplyfyRouteToWaypoint();
            if (RouteToWaypoint.Count == 0)
            {
                RouteToWaypoint.Push(wayPoints.Peek());
            }

            stuckDetector.SetTargetLocation(RouteToWaypoint.Peek());
        }

        private void RefillWaypoints(bool findClosest = false)
        {
            LogDebug($"RefillWaypoints firstLoad:{firstLoad} - findClosest:{findClosest} - ThereAndBack:{input.ClassConfig.PathThereAndBack}");

            if (firstLoad)
            {
                firstLoad = false;
                var closestPoint = routePoints.OrderBy(p => playerReader.PlayerLocation.DistanceXYTo(p)).FirstOrDefault();

                for (int i = 0; i < routePoints.Count; i++)
                {
                    wayPoints.Push(routePoints[i]);
                    if (routePoints[i] == closestPoint) { break; }
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
            if (useClassConfigRule && input.ClassConfig.PathThereAndBack)
            {
                var player = playerReader.PlayerLocation;
                var distanceToFirst = player.DistanceXYTo(routePoints[0]);
                var distanceToLast = player.DistanceXYTo(routePoints[^1]);

                if (distanceToLast > distanceToFirst)
                {
                    var reversed = routePoints.ToList();
                    reversed.Reverse();
                    reversed.ForEach(p => wayPoints.Push(p));
                }
                else
                {
                    routePoints.ForEach(p => wayPoints.Push(p));
                }
            }
            else
            {
                var reversed = routePoints.ToList();
                reversed.Reverse();
                reversed.ForEach(p => wayPoints.Push(p));
            }
        }

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(RouteToWaypoint.ToArray(), 0.05f);
            simple.Reverse();

            RouteToWaypoint.Clear();
            simple.ForEach((x) => RouteToWaypoint.Push(x));
        }


        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }

        private void LogDebug(string text)
        {
            if (debug)
            {
                logger.LogDebug($"{nameof(Navigation)}: {text}");
            }
        }

        private void Log(string text)
        {
            logger.LogInformation($"{nameof(Navigation)}: {text}");
        }

    }
}