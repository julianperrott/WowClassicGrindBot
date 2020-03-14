using Libs.GOAP;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using System.IO;

namespace Libs.Actions
{
    public class WalkToCorpseAction : GoapAction
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private double lastDistance = 999;
        private readonly List<WowPoint> spiritWalkerPath;
        private readonly List<WowPoint> routePoints;
        private Stack<WowPoint> points = new Stack<WowPoint>();
        private Stopwatch LastReachedPoint = new Stopwatch();

        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        public WowPoint? NextPoint()
        {
            return points.Count == 0 ? null : points.Peek();
        }

        public WalkToCorpseAction(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection,List<WowPoint> spiritWalker,List<WowPoint> routePoints, StopMoving stopMoving)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.routePoints = routePoints.ToList();
            this.spiritWalkerPath= spiritWalker.ToList();

            AddPrecondition(GoapKey.isdead, true);
        }

        public override float CostOfPerformingAction { get => 1f; }

        public WowPoint CorpseLocation => new WowPoint(playerReader.CorpseX, playerReader.CorpseY);

        public void Dump(string description)
        {
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = DistanceTo(location, CorpseLocation);
            var heading = new DirectionCalculator().CalculateHeading(location, CorpseLocation);
            //Debug.WriteLine($"{description}: Point {index}, Distance: {distance} ({lastDistance}), heading: {playerReader.Direction}, best: {heading}");
        }

        private bool NeedsToReset = true;


        public override void OnActionEvent(object sender, ActionEvent e)
        {
            NeedsToReset = true;
            if (sender != this)
            {
                LastReachedPoint.Reset();
            }
        }

        public override async Task PerformAction()
        {
            if (!LastReachedPoint.IsRunning) { LastReachedPoint.Start(); }

            if (NeedsToReset)
            {
                await Reset();
            }

            await Task.Delay(200);
            //wowProcess.SetKeyState(ConsoleKey.UpArrow, true);

            if (!this.playerReader.PlayerBitValues.DeadStatus){ return; }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            double distance = 0;
            double heading = 0;

            if (points.Count == 0)
            {
                distance = DistanceTo(location, CorpseLocation);
                heading = new DirectionCalculator().CalculateHeading(location, CorpseLocation);
            }
            else
            {
                distance = DistanceTo(location, points.Peek());
                heading = new DirectionCalculator().CalculateHeading(location, points.Peek());
            }

            if (lastDistance < distance)
            {
                Dump("Further away");
                playerDirection.SetDirection(heading);
            }
            else if (lastDistance == distance)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                await Task.Delay(100);
                // stuck so jump
                if ((DateTime.Now - LastActive).TotalSeconds < 2)
                {
                    await wowProcess.KeyPress(ConsoleKey.Spacebar, 500);
                    Dump("Stuck");

                    if (LastReachedPoint.ElapsedMilliseconds > 1000 * 120)
                    {
                        // stuck for 2 minutes
                        Debug.WriteLine("Stuck for 2 minutes");
                        RaiseEvent(new ActionEvent(GoapKey.abort, true));
                    }
                }
                else
                {
                    Dump("Resuming movement");
                }
            }
            else // distance closer
            {
                Dump("Closer");
                //playerDirection.SetDirection(heading);

                var diff1 = Math.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
                var diff2 = Math.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

                if (Math.Min(diff1, diff2) > 0.3)
                {
                    Dump("Correcting direction");
                    playerDirection.SetDirection(heading);
                }
            }

            lastDistance = distance;

            if (distance < 40 && points.Any())
            {
                Debug.WriteLine($"Move to next point");
                LastReachedPoint.Reset();
                points.Pop();
                lastDistance = 999;
                if (points.Count > 0)
                {
                    heading = new DirectionCalculator().CalculateHeading(location, points.Peek());
                    playerDirection.SetDirection(heading);
                }
            }

            LastActive = DateTime.Now;
        }

        public async Task Reset()
        {
            Debug.WriteLine("Sleeping 10 seconds");
            await Task.Delay(10000);
            while(new List<double> { playerReader.XCoord, playerReader.YCoord, CorpseLocation.X,CorpseLocation.Y }.Max()>100)
            {
                Debug.WriteLine($"Waiting... odd coords read. Player {playerReader.XCoord},{playerReader.YCoord} corpse { CorpseLocation.X}{CorpseLocation.Y}");
                await Task.Delay(5000);
            }


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


            var closestRoutePointToCorpse = routePoints.Select(s => (pathPoint: s, distance: DistanceTo(s, CorpseLocation)))
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

            var cp = new CorpsePath { MyLocation = myLocation, CorpseLocation = CorpseLocation, RouteToCorpse = routeToCorpse, TruncatedRoute = truncatedRoute };
            File.WriteAllText($"../../../../CorpsePath_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json", JsonConvert.SerializeObject(cp));
            NeedsToReset = false;
        }

        public class CorpsePath
        {
            public WowPoint MyLocation { get; set; } = new WowPoint(0, 0);
            public WowPoint CorpseLocation { get; set; } = new WowPoint(0, 0);

            public List<WowPoint> RouteToCorpse { get; set; } = new List<WowPoint>();
            public List<WowPoint> TruncatedRoute { get; set; } = new List<WowPoint>();
        }

        private static List<WowPoint> FillPathToCorpse( WowPoint closestRoutePointToCorpse, WowPoint pathStartPoint, List<WowPoint> routePoints)
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

        public bool IsDone()
        {
            return false;
        }

        private double DistanceTo(WowPoint l1, WowPoint l2)
        {
            var x = l1.X - l2.X;
            var y = l1.Y - l2.Y;
            x = x * 100;
            y = y * 100;
            var distance = Math.Sqrt((x * x) + (y * y));

            //Debug.WriteLine($"distance:{x} {y} {distance.ToString()}");
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
    }
}