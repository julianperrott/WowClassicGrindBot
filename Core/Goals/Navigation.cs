using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;

namespace Core.Goals
{
    internal readonly struct PathRequest
    {
        public int MapId { init; get; }
        public Vector3 Start { init; get; }
        public Vector3 End { init; get; }
        public Action<List<Vector3>, bool> Callback { init; get; }

        public PathRequest(int mapId, Vector3 start, Vector3 end, Action<List<Vector3>, bool> callback)
        {
            MapId = mapId;
            Start = start;
            End = end;
            Callback = callback;
        }
    }

    internal readonly struct PathResult
    {
        public List<Vector3> Path { init; get; }
        public bool Success { init; get; }
        public Action<List<Vector3>, bool> Callback { init; get; }

        public PathResult(List<Vector3> path, bool success, Action<List<Vector3>, bool> callback)
        {
            Path = path;
            Success = success;
            Callback = callback;
        }
    }

    public class Navigation
    {
        private readonly bool debug = false;
        private readonly float RADIAN = MathF.PI * 2;

        private readonly ILogger logger;
        private readonly IPlayerDirection playerDirection;
        private readonly ConfigurableInput input;
        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly StuckDetector stuckDetector;
        private readonly IPPather pather;
        private readonly MountHandler mountHandler;

        private readonly int MinDistance = 10;
        private readonly int MinDistanceMount = 15;
        private readonly int MaxDistance = 200;

        private float AvgDistance;
        private float lastDistance = float.MaxValue;

        private readonly float minAngleToTurn = MathF.PI / 35;          // 5.14 degree
        private readonly float minAngleToStopBeforeTurn = MathF.PI / 3; // 60 degree

        private readonly Stack<Vector3> wayPoints = new();
        private readonly Stack<Vector3> routeToNextWaypoint = new();

        public List<Vector3> TotalRoute { private init; get; } = new();

        public DateTime LastActive { get; private set; }

        public event EventHandler? OnPathCalculated;
        public event EventHandler? OnWayPointReached;
        public event EventHandler? OnDestinationReached;

        public bool SimplifyRouteToWaypoint { get; set; } = true;

        private bool active;

        private readonly ConcurrentQueue<PathResult> pathResults = new();
        private Thread? pathfinderThread;

        public Navigation(ILogger logger, IPlayerDirection playerDirection, ConfigurableInput input, AddonReader addonReader, StopMoving stopMoving, StuckDetector stuckDetector, IPPather pather, MountHandler mountHandler)
        {
            this.logger = logger;
            this.playerDirection = playerDirection;
            this.input = input;
            this.addonReader = addonReader;
            playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;
            this.stuckDetector = stuckDetector;
            this.pather = pather;
            this.mountHandler = mountHandler;

            AvgDistance = MinDistance;
        }

        public void Update()
        {
            active = true;

            if (wayPoints.Count == 0 && routeToNextWaypoint.Count == 0)
            {
                OnDestinationReached?.Invoke(this, EventArgs.Empty);
                return;
            }

            while (pathResults.TryDequeue(out PathResult result))
            {
                result.Callback(result.Path, result.Success);
            }

            if (pathfinderThread != null)
            {
                return;
            }

            if (routeToNextWaypoint.Count == 0)
            {
                RefillRouteToNextWaypoint();
                return;
            }

            LastActive = DateTime.UtcNow;
            input.SetKeyState(input.ForwardKey, true, false);

            // main loop
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(routeToNextWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, routeToNextWaypoint.Peek());

            if (distance < ReachedDistance(MinDistance))
            {
                if (routeToNextWaypoint.Count > 0)
                {
                    if (routeToNextWaypoint.Peek().Z != 0 && routeToNextWaypoint.Peek().Z != location.Z)
                    {
                        playerReader.ZCoord = routeToNextWaypoint.Peek().Z;
                        if (debug)
                            LogDebug($"Update PlayerLocation.Z = {playerReader.ZCoord}");
                    }

                    if (SimplifyRouteToWaypoint)
                        ReduceByDistance(MinDistance);
                    else
                        routeToNextWaypoint.Pop();

                    lastDistance = float.MaxValue;
                    UpdateTotalRoute();
                }

                if (routeToNextWaypoint.Count == 0)
                {
                    if (wayPoints.Count > 0)
                    {
                        routeToNextWaypoint.Push(wayPoints.Pop());
                        stuckDetector.SetTargetLocation(routeToNextWaypoint.Peek());
                        UpdateTotalRoute();
                    }

                    if (debug)
                        LogDebug($"Move to next wayPoint! Remains: {wayPoints.Count} -- distance: {distance}");

                    OnWayPointReached?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    stuckDetector.SetTargetLocation(routeToNextWaypoint.Peek());

                    heading = DirectionCalculator.CalculateHeading(location, routeToNextWaypoint.Peek());
                    AdjustHeading(heading, "Turn to next point");
                    return;
                }
            }

            if (routeToNextWaypoint.Count > 0)
            {
                if (!stuckDetector.IsGettingCloser())
                {
                    if (lastDistance < distance)
                    {
                        // TODO: test this
                        AdjustNextWaypointPointToClosest();
                        AdjustHeading(heading, "unstuck Further away");
                    }

                    if (HasBeenActiveRecently())
                    {
                        stuckDetector.Unstick();
                        distance = location.DistanceXYTo(routeToNextWaypoint.Peek());
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

        public void Resume()
        {
            if (pather is not RemotePathingAPIV3 && routeToNextWaypoint.Count > 0)
            {
                V1_AttemptToKeepRouteToWaypoint();
            }

            var removed = 0;
            while (AdjustNextWaypointPointToClosest() && removed < 5) { removed++; };
            if (removed > 0)
            {
                if (debug)
                    LogDebug($"Resume: removed {removed} waypoint!");
            }
        }

        public void Stop()
        {
            active = false;

            if (pather is RemotePathingAPIV3)
                routeToNextWaypoint.Clear();

            ResetStuckParameters();
        }

        public bool HasWaypoint()
        {
            return wayPoints.Count != 0;
        }

        public bool HasNext()
        {
            return routeToNextWaypoint.Count != 0;
        }

        public Vector3 NextPoint()
        {
            return routeToNextWaypoint.Peek();
        }

        public void SetWayPoints(List<Vector3> points)
        {
            wayPoints.Clear();
            routeToNextWaypoint.Clear();

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

            if (debug)
                LogDebug($"SetWayPoints: Added {wayPoints.Count} - AvgDistance: {AvgDistance}");

            UpdateTotalRoute();
        }

        public void ResetStuckParameters()
        {
            stuckDetector.ResetStuckParameters();
        }

        private void RefillRouteToNextWaypoint()
        {
            routeToNextWaypoint.Clear();

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(wayPoints.Peek());
            if (distance > MaxDistance || distance > (AvgDistance + MinDistance))
            {
                stopMoving.Stop();

                Log($"pathfinder - {distance} - {location} -> {wayPoints.Peek()}");
                PathRequest(new PathRequest(addonReader.UIMapId.Value, location, wayPoints.Peek(), (List<Vector3> path, bool success) =>
                {
                    if (!active)
                        return;

                    if (!success || path == null)
                    {
                        LogWarn($"Unable to find path {location} -> {wayPoints.Peek()}. Character may stuck!");
                        return;
                    }

                    if (path.Count == 0)
                    {
                        LogWarn($"Unable to find path {location} -> {wayPoints.Peek()}. Character may stuck!");
                    }

                    path.Reverse();
                    path.ForEach(p => routeToNextWaypoint.Push(p));

                    if (SimplifyRouteToWaypoint)
                        SimplyfyRouteToWaypoint();

                    if (routeToNextWaypoint.Count == 0)
                    {
                        routeToNextWaypoint.Push(wayPoints.Peek());

                        if (debug)
                            LogDebug($"RefillRouteToNextWaypoint -- WayPoint reached! {wayPoints.Count}");
                    }

                    stuckDetector.SetTargetLocation(routeToNextWaypoint.Peek());
                    UpdateTotalRoute();

                    OnPathCalculated?.Invoke(this, EventArgs.Empty);
                }));
            }
            else
            {
                Log($"non pathfinder - {distance} - {location} -> {wayPoints.Peek()}");

                routeToNextWaypoint.Push(wayPoints.Peek());

                var heading = DirectionCalculator.CalculateHeading(location, wayPoints.Peek());
                AdjustHeading(heading, "Reached waypoint");

                stuckDetector.SetTargetLocation(routeToNextWaypoint.Peek());
                UpdateTotalRoute();
            }
        }

        private void PathRequest(PathRequest pathRequest)
        {
            pathfinderThread = new Thread(async () =>
            {
                var path = await pather.FindRoute(pathRequest.MapId, pathRequest.Start, pathRequest.End);
                if (active)
                {
                    pathResults.Enqueue(new PathResult(path, true, pathRequest.Callback));
                }
                pathfinderThread = null;
            });
            pathfinderThread.Start();
        }

        private int ReachedDistance(int minDistance)
        {
            return mountHandler.IsMounted() ? MinDistanceMount : minDistance;
        }

        private void ReduceByDistance(int minDistance)
        {
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(routeToNextWaypoint.Peek());
            while (distance < ReachedDistance(minDistance) && routeToNextWaypoint.Count > 0)
            {
                routeToNextWaypoint.Pop();
                if (routeToNextWaypoint.Count > 0)
                {
                    distance = location.DistanceXYTo(routeToNextWaypoint.Peek());
                }
            }
        }

        private void AdjustHeading(float heading, string source = "")
        {
            var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var diff = MathF.Min(diff1, diff2);
            if (diff > minAngleToTurn)
            {
                if (diff > minAngleToStopBeforeTurn)
                {
                    stopMoving.Stop();
                }

                playerDirection.SetDirection(heading, routeToNextWaypoint.Peek(), source, MinDistance);
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
                if (debug)
                    LogDebug("Adjusted resume point");

                return false;
            }

            if (debug)
                LogDebug("Skipped next point in path");

            return true;
        }

        private void V1_AttemptToKeepRouteToWaypoint()
        {
            float totalDistance = TotalRoute.Zip(TotalRoute.Skip(1), VectorExt.DistanceXY).Sum();
            if (totalDistance > MaxDistance / 2)
            {
                var location = playerReader.PlayerLocation;
                float distance = location.DistanceXYTo(routeToNextWaypoint.Peek());
                if (distance > 2 * MinDistanceMount)
                {
                    Log($"[{pather.GetType().Name}] distance from nearlest point is {distance}. Have to clear RouteToWaypoint.");
                    routeToNextWaypoint.Clear();
                }
                else
                {
                    Log($"[{pather.GetType().Name}] distance is close {distance}. Keep RouteToWaypoint.");
                }
            }
            else
            {
                Log($"[{pather.GetType().Name}] total distance {totalDistance}<{MaxDistance / 2}. Have to clear RouteToWaypoint.");
                routeToNextWaypoint.Clear();
            }
        }

        private void SimplyfyRouteToWaypoint()
        {
            var simple = PathSimplify.Simplify(routeToNextWaypoint.ToArray(), pather is RemotePathingAPIV3 ? 0.05f : 0.1f);
            simple.Reverse();

            routeToNextWaypoint.Clear();
            simple.ForEach((x) => routeToNextWaypoint.Push(x));
        }

        private void UpdateTotalRoute()
        {
            TotalRoute.Clear();
            TotalRoute.AddRange(routeToNextWaypoint);
            TotalRoute.AddRange(wayPoints);
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.UtcNow - LastActive).TotalSeconds < 2;
        }


        private void LogDebug(string text)
        {
            logger.LogDebug($"{nameof(Navigation)}: {text}");
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