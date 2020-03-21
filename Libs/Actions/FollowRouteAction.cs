using Libs.GOAP;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Libs.NpcFinder;
using System.Runtime.InteropServices;
using PInvoke;
using Microsoft.Extensions.Logging;

namespace Libs.Actions
{
    public class FollowRouteAction : GoapAction
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly List<WowPoint> pointsList;
        private Stack<WowPoint> points=new Stack<WowPoint>();
        public WowPoint? NextPoint()
        {
            return points.Count==0 ? null: points.Peek();
        }

        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;
        private double lastDistance = 999;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);
        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        private DateTime lastTab = DateTime.Now;
        private readonly List<string> blacklist;
        private bool shouldMount = true;
        private ILogger logger;

        private Stopwatch LastReachedPoint = new Stopwatch();

        public FollowRouteAction(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, List<WowPoint> points, StopMoving stopMoving, NpcNameFinder npcNameFinder, List<string> blacklist, ILogger logger)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.pointsList = points;
            this.npcNameFinder = npcNameFinder;
            this.blacklist = blacklist;
            this.logger = logger;

            AddPrecondition(GoapKey.incombat, false);
        }

        private void RefillPoints(bool findClosest = false)
        {
            if (findClosest)
            {
                pointsList.ForEach(p => points.Push(p));
                AdjustNextPointToClosest();
            }
            else
            {
                pointsList.ForEach(p => points.Push(p));
            }
        }

        public override float CostOfPerformingAction { get => 20f; }
        

        public void Dump(string description)
        {
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, points.Peek());
            var heading = new DirectionCalculator(logger).CalculateHeading(location, points.Peek());
            //logger.LogInformation($"{description}: Point {index}, Distance: {distance} ({lastDistance}), heading: {playerReader.Direction}, best: {heading}");
        }

        public override void OnActionEvent(object sender, ActionEvent e)
        {
            if (sender!=this)
            {
                shouldMount = true;
                LastReachedPoint.Reset();
            }
        }

        public override async Task PerformAction()
        {
            if (!LastReachedPoint.IsRunning) { LastReachedPoint.Start(); }

            RaiseEvent(new ActionEvent(GoapKey.fighting, false));

            if (points.Count == 0)
            {
                RefillPoints(true);
            }

            await Task.Delay(200);
            //wowProcess.SetKeyState(ConsoleKey.UpArrow, true);

            if (this.playerReader.PlayerBitValues.PlayerInCombat) { return; }

            if ((DateTime.Now - LastActive).TotalSeconds > 10)
            {
                var pointsRemoved = 0;
                while (AdjustNextPointToClosest() && pointsRemoved < 5) { pointsRemoved++; };
            }

            await RandomJump();

            // press tab
            if (!this.playerReader.PlayerBitValues.PlayerInCombat && (DateTime.Now - lastTab).TotalMilliseconds > 1100)
            {
                //new PressKeyThread(this.wowProcess, ConsoleKey.Tab);
                if (await LookForTarget()) { return; }
            }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, points.Peek());
            var heading = new DirectionCalculator(logger).CalculateHeading(location, points.Peek());

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, points.Peek(), "Further away");
            }
            else if (lastDistance == distance)
            {
                Dump("Stuck");
                // stuck so jump
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await Unstick();
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
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
                    await playerDirection.SetDirection(heading, points.Peek(), "Correcting direction");
                }
            }

            lastDistance = distance;

            if (distance < 50)
            {
                logger.LogInformation($"Move to next point");
                LastReachedPoint.Reset();
                points.Pop();
                lastDistance = 999;
                if (points.Count == 0)
                {
                    RefillPoints();
                }

                heading = new DirectionCalculator(logger).CalculateHeading(location, points.Peek());
                await playerDirection.SetDirection(heading, points.Peek(), "Move to next point");
            }

            // should mount
            if (shouldMount)
            {
                shouldMount = false;

                if (await LookForTarget()) { return; }

                if (this.npcNameFinder.CountNpc(0) == 0)
                {
                    logger.LogInformation("Mounting if level >=40 no NPC in sight");
                    if (this.playerReader.Level >= 40)
                    {
                        await wowProcess.Mount(this.playerReader);
                    }
                }
                else
                {
                    logger.LogInformation("Not mounting as can see NPC");
                }
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
            }

            LastActive = DateTime.Now;
        }

        private async Task<bool> LookForTarget()
        {
            await this.wowProcess.KeyPress(ConsoleKey.Tab, 300);
            await Task.Delay(300);
            if (!playerReader.HasTarget)
            {
                await this.npcNameFinder.FindAndClickNpc(0);
                await Task.Delay(300);
            }

            return this.playerReader.HasTarget && !blacklist.Contains(playerReader.Target);
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }

        private bool AdjustNextPointToClosest()
        {
            if (points.Count < 2) { return false; }

            var A = points.Pop();
            var B = points.Peek();
            var result = GetClosestPointOnLineSegment(A.Vector2(), B.Vector2(), new Vector2((float)this.playerReader.XCoord, (float)this.playerReader.YCoord));
            var newPoint = new WowPoint(result.X, result.Y);
            if (WowPoint.DistanceTo(newPoint, points.Peek()) >= 4)
            {
                points.Push(newPoint);
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

        private async Task Unstick()
        {
            await wowProcess.KeyPress(ConsoleKey.Spacebar, 500);
            Dump("Stuck");

            int stuckSeconds = (int)(LastReachedPoint.ElapsedMilliseconds / 1000);

            if (stuckSeconds > 240)
            {
                // stuck for 4 minutes
                logger.LogInformation("Stuck for 4 minutes");
                RaiseEvent(new ActionEvent(GoapKey.abort, true));
            }

            if (stuckSeconds > 30)
            {
                // stuck for 30 seconds
                logger.LogInformation("Stuck for over 90 seconds");
                var r = random.Next(0, 100);
                if (r<25)
                {
                    wowProcess.SetKeyState(ConsoleKey.Q, true);
                    await Task.Delay(5 * 1000);
                    wowProcess.SetKeyState(ConsoleKey.Q, false);
                }
                else if(r > 75)
                {
                    wowProcess.SetKeyState(ConsoleKey.E, true);
                    await Task.Delay(5 * 1000);
                    wowProcess.SetKeyState(ConsoleKey.E, false);
                }
            }
        }

    }
}