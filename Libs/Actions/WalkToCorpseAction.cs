using Libs.GOAP;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;

namespace Libs.Actions
{
    public class WalkToCorpseAction : GoapAction
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private double lastDistance = 999;

        public WalkToCorpseAction(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;

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

        private DateTime lastTab = DateTime.Now;

        public override async Task PerformAction()
        {
            await Task.Delay(200);
            //wowProcess.SetKeyState(ConsoleKey.UpArrow, true);

            if (!this.playerReader.PlayerBitValues.DeadStatus){ return; }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = DistanceTo(location, CorpseLocation);
            var heading = new DirectionCalculator().CalculateHeading(location, CorpseLocation);

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