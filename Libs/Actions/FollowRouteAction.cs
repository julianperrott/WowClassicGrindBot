using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class FollowRouteAction : GoapAction
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly List<WowPoint> points;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private int index = 0;
        private double lastDistance = 999;

        public FollowRouteAction(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, List<WowPoint> points)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.points = points;

            AddPrecondition(GoapKey.incombat, false);
        }

        public override float CostOfPerformingAction { get => 20f; }

        public void Dump(string description)
        {
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = DistanceTo(location, points[index]);
            var heading = new DirectionCalculator().CalculateHeading(location, points[index]);
            //Debug.WriteLine($"{description}: Point {index}, Distance: {distance} ({lastDistance}), heading: {playerReader.Direction}, best: {heading}");
        }

        private DateTime lastTab = DateTime.Now;

        public override async Task PerformAction()
        {
            await Task.Delay(200);
            //wowProcess.SetKeyState(ConsoleKey.UpArrow, true);

            // press tab
            if (!this.playerReader.PlayerBitValues.PlayerInCombat && (DateTime.Now - lastTab).TotalMilliseconds > 1100)
            {
                //new PressKeyThread(this.wowProcess, ConsoleKey.Tab);
                this.wowProcess.SetKeyState(ConsoleKey.Tab, true);
                Thread.Sleep(420);
                this.wowProcess.SetKeyState(ConsoleKey.Tab, false);
            }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = DistanceTo(location, points[index]);
            var heading = new DirectionCalculator().CalculateHeading(location, points[index]);

            if (lastDistance < distance)
            {
                Dump("Further away");
                playerDirection.SetDirection(heading);
            }
            else if (lastDistance == distance)
            {
                Dump("Stuck");
                // stuck so jump
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                await Task.Delay(100);
                wowProcess.SetKeyState(ConsoleKey.Spacebar, true);
                await Task.Delay(500);
                wowProcess.SetKeyState(ConsoleKey.Spacebar, false);
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

            if (distance < 4)
            {
                Debug.WriteLine($"Move to next point");
                index++;
                lastDistance = 999;
                if (index >= points.Count)
                {
                    index = 0;
                }

                heading = new DirectionCalculator().CalculateHeading(location, points[index]);
                playerDirection.SetDirection(heading);
            }
        }

        public void Reset()
        {
            wowProcess.SetKeyState(ConsoleKey.UpArrow, false);
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

        public override void ResetBeforePlanning()
        {
        }

        public override bool IsActionDone()
        {
            return false;
        }

        public override bool CheckIfActionCanRun()
        {
            return true;
        }

        public override bool NeedsToBeInRangeOfTargetToExecute()
        {
            return false;
        }

        public override void Abort()
        {
            wowProcess.SetKeyState(ConsoleKey.UpArrow, false);
        }
    }
}