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

        private readonly int PreciseMinDistance = 5;
        private readonly int MinDistance = 10;
        private readonly int MinDistanceMount = 15;
        private readonly int MaxDistance = 200;

        private float AvgDistance;
        private float lastDistance = float.MaxValue;

        private readonly Stack<Vector3> wayPoints = new();
        public Stack<Vector3> RouteToWaypoint { private init; get; } = new();

        public List<Vector3> TotalRoute { private init; get; } = new();

        public DateTime LastActive { get; set; }

        public event EventHandler? OnWayPointReached;
        public event EventHandler? OnDestinationReached;

        public bool PreciseMovement { get; set; }
        public bool PreciseEnd { get; set; }

        public bool SimplifyRouteToWaypoint { get; set; } = true;

        public Navigation(ILogger logger, IPlayerDirection playerDirection, ConfigurableInput input, AddonReader addonReader, Wait wait, StopMoving stopMoving, StuckDetector stuckDetector, IPPather pather, MountHandler mountHandler)
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

            AvgDistance = MinDistance;
        }

        public async ValueTask Update()
        {
            LastActive = DateTime.Now;

            if (wayPoints.Count == 0 && RouteToWaypoint.Count == 0)
            {
                OnDestinationReached?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (RouteToWaypoint.Count == 0)
            {
                await RefillRouteToNextWaypoint(false);

                if (RouteToWaypoint.Count == 0)
                {
                    LogWarn("No RouteToWaypoint available!");
                    stopMoving.Stop();
                    return;
                }
            }

            input.SetKeyState(input.ForwardKey, true, false);

            // main loop
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(RouteToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, RouteToWaypoint.Peek());

            if (distance < ReachedDistance(MinDistance))
            {
                if (RouteToWaypoint.Count > 0)
                {
                    if (RouteToWaypoint.Peek().Z != 0 && RouteToWaypoint.Peek().Z != location.Z)
                    {
                        playerReader.ZCoord = RouteToWaypoint.Peek().Z;
                        LogDebug($"Update PlayerLocation.Z = {playerReader.ZCoord}");
                    }

                    if (SimplifyRouteToWaypoint)
                        ReduceByDistance(MinDistance);
                    else
                        RouteToWaypoint.Pop();

                    UpdateTotalRoute();
                }

                if (RouteToWaypoint.Count == 0)
                {
                    if (wayPoints.Count > 0)
                    {
                        if (PreciseEnd)
                        {
                            RouteToWaypoint.Push(wayPoints.Pop());
                            stuckDetector.SetTargetLocation(RouteToWaypoint.Peek());
                        }
                        else
                        {
                            wayPoints.Pop();
                        }
                        
                        UpdateTotalRoute();
                    }

                    LogDebug($"Move to next wayPoint! Remains: {wayPoints.Count} -- distance: {distance}");
                    OnWayPointReached?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    stuckDetector.SetTargetLocation(RouteToWaypoint.Peek());

                    heading = DirectionCalculator.CalculateHeading(location, RouteToWaypoint.Peek());
                    AdjustHeading(heading, "Turn to next point");
                    return;
                }
            }

            if (RouteToWaypoint.Count > 0)
            {
                if (!stuckDetector.IsGettingCloser())
                {
                    if (lastDistance < distance)
                    {
                        if (!PreciseMovement)
                        {
                            AdjustNextWaypointPointToClosest();
                        }

                        AdjustHeading(heading, "Further away");
                    }

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
        }

        private int ReachedDistance(int distance)
        {
            if (mountHandler.IsMounted())
            {
                return PreciseMovement ? PreciseMinDistance : MinDistanceMount;
            }
            else
            {
                return PreciseMovement ? PreciseMinDistance : distance;
            }
        }

        private void ReduceByDistance(int minDistance)
        {
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(RouteToWaypoint.Peek());
            while (distance < ReachedDistance(minDistance) && RouteToWaypoint.Count > 0) // -1
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
            if (diff > wanderAngle || PreciseMovement)
            {
                if (diff > Math.PI / 2)
                {
                    stopMoving.Stop();
                }

                playerDirection.SetDirection(heading, RouteToWaypoint.Peek(), source, PreciseMovement ? PreciseMinDistance : MinDistance);
            }
        }

        private bool AdjustNextWaypointPointToClosest()
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

        public void Resume()
        {
            var removed = 0;
            while (AdjustNextWaypointPointToClosest() && removed < 5) { removed++; };
            if (removed > 0)
            {
                LogDebug($"Resume: removed {removed} waypoint!");
            }
        }

        public void Stop()
        {
            RouteToWaypoint.Clear();
            ResetStuckParameters();
        }

        public bool HasWaypoint()
        {
            return wayPoints.Count > 0;
        }

        public async void SetWayPoints(List<Vector3> points)
        {
            wayPoints.Clear();
            RouteToWaypoint.Clear();

            points.Reverse();
            points.ForEach(x => wayPoints.Push(x));

            if (wayPoints.Count > 1)
            {
                float sum = 0;
                sum = points.Zip(points.Skip(1), (a, b) => a.DistanceXYTo(b)).Sum();
                AvgDistance = sum / points.Count;
            }
            else
            {
                AvgDistance = MinDistance;
            }
            LogDebug($"SetWayPoints: Added {wayPoints.Count} - AvgDistance: {AvgDistance}");

            await RefillRouteToNextWaypoint(false);

            UpdateTotalRoute();
        }

        public void ResetStuckParameters()
        {
            stuckDetector.ResetStuckParameters();
        }

        public async ValueTask RefillRouteToNextWaypoint(bool forceUsePathing)
        {
            RouteToWaypoint.Clear();

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(wayPoints.Peek());
            if (forceUsePathing || (!PreciseMovement && distance > (AvgDistance + MinDistance)) || distance > MaxDistance)
            {
                Log($"RefillRouteToNextWaypoint - {distance} - ask pathfinder {location} -> {wayPoints.Peek()}");

                stopMoving.Stop();
                var path = await pather.FindRouteTo(addonReader, wayPoints.Peek());
                if (path.Count == 0)
                {
                    LogWarn($"Unable to find path from {location} -> {wayPoints.Peek()}. Character may stuck!");
                }

                path.Reverse();
                path.ForEach(p => RouteToWaypoint.Push(p));

                if (SimplifyRouteToWaypoint)
                    SimplyfyRouteToWaypoint();

                if (RouteToWaypoint.Count == 0)
                {
                    RouteToWaypoint.Push(wayPoints.Peek());
                    LogDebug($"RefillRouteToNextWaypoint -- WayPoint reached! {wayPoints.Count}");
                }
            }
            else
            {
                RouteToWaypoint.Push(wayPoints.Peek());

                var heading = DirectionCalculator.CalculateHeading(location, wayPoints.Peek());
                AdjustHeading(heading, "Reached waypoint");
            }

            stuckDetector.SetTargetLocation(RouteToWaypoint.Peek());
        }

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(RouteToWaypoint.ToArray(), pather is RemotePathingAPIV3 ? 0.05f : 0.1f);
            simple.Reverse();

            RouteToWaypoint.Clear();
            simple.ForEach((x) => RouteToWaypoint.Push(x));
        }


        private void UpdateTotalRoute()
        {
            TotalRoute.Clear();
            TotalRoute.AddRange(RouteToWaypoint);
            TotalRoute.AddRange(wayPoints);
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

        private void LogWarn(string text)
        {
            logger.LogWarning($"{nameof(Navigation)}: {text}");
        }
    }
}