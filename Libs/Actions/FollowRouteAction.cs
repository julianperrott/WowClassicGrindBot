using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class FollowRouteAction : GoapAction
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly List<WowPoint> pointsList;
        private Stack<WowPoint> points = new Stack<WowPoint>();

        public WowPoint? NextPoint()
        {
            return points.Count == 0 ? null : points.Peek();
        }

        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private double lastDistance = 999;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);
        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        private DateTime lastTab = DateTime.Now;
        private readonly IBlacklist blacklist;
        private bool shouldMount = true;
        private ILogger logger;

        private bool firstLoad = true;

        public FollowRouteAction(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, List<WowPoint> points, StopMoving stopMoving, NpcNameFinder npcNameFinder, IBlacklist blacklist, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.pointsList = points;
            this.npcNameFinder = npcNameFinder;
            this.blacklist = blacklist;
            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;

            if (!this.classConfiguration.GatherOnly)
            {
                AddPrecondition(GoapKey.incombat, false);
            }
        }

        private void RefillPoints(bool findClosest = false)
        {
            if (firstLoad)
            {
                // start path at closest point
                firstLoad = false;
                var me = this.playerReader.PlayerLocation;
                var closest = pointsList.OrderBy(p => WowPoint.DistanceTo(me, p)).FirstOrDefault();

                for (int i = 0; i < pointsList.Count; i++)
                {
                    points.Push(pointsList[i]);
                    if (pointsList[i] == closest) { break; }
                }
            }
            else
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
        }

        public override float CostOfPerformingAction { get => 20f; }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                shouldMount = true;
            }
        }

        private int lastGatherKey = 0;
        private DateTime lastGatherClick = DateTime.Now.AddSeconds(-10);

        public override async Task PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));
            if (this.classConfiguration.GatherOnly && this.lastGatherClick.AddSeconds(3)<DateTime.Now && this.classConfiguration.GatherFindKeyConfig.Count>0)
            {
                lastGatherKey++;
                if (lastGatherKey>= this.classConfiguration.GatherFindKeyConfig.Count)
                {
                    lastGatherKey = 0;
                }

                await wowProcess.KeyPress(classConfiguration.GatherFindKeyConfig[lastGatherKey].ConsoleKey, 200, "Gatherkey 1");
                lastGatherClick = DateTime.Now;
            }

            if (points.Count == 0)
            {
                RefillPoints(true);
                this.stuckDetector.SetTargetLocation(this.points.Peek());
            }

            await Task.Delay(200);
            wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 1");

            if (this.playerReader.PlayerBitValues.PlayerInCombat && !this.classConfiguration.GatherOnly) { return; }

            if ((DateTime.Now - LastActive).TotalSeconds > 10)
            {
                var pointsRemoved = 0;
                this.stuckDetector.SetTargetLocation(this.points.Peek());
                while (AdjustNextPointToClosest() && pointsRemoved < 5) { pointsRemoved++; };
            }

            await RandomJump();

            if (!this.classConfiguration.GatherOnly)
            {
                // press tab
                if (!this.playerReader.PlayerBitValues.PlayerInCombat && (DateTime.Now - lastTab).TotalMilliseconds > 1100)
                {
                    //new PressKeyThread(this.wowProcess, ConsoleKey.Tab);
                    if (await LookForTarget())
                    {
                        if (this.playerReader.HasTarget)
                        {
                            logger.LogInformation("Has target!");
                            return;
                        }
                    }
                }
            }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, points.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, points.Peek());

            if (this.classConfiguration.GatherOnly)
            {
                await AdjustHeading(heading);
            }

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, points.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 2");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await this.stuckDetector.Unstick();
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                await AdjustHeading(heading);
            }

            lastDistance = distance;

            if (distance < PointReachedDistance())
            {
                logger.LogInformation($"Move to next point");

                points.Pop();
                lastDistance = 999;
                if (points.Count == 0)
                {
                    RefillPoints();
                }

                this.stuckDetector.SetTargetLocation(this.points.Peek());

                heading = DirectionCalculator.CalculateHeading(location, points.Peek());
                await playerDirection.SetDirection(heading, points.Peek(), "Move to next point");

                if (this.classConfiguration.Blink.ConsoleKey != 0 && this.playerReader.ManaPercentage > 90 && this.playerReader.PlayerLevel < 40)
                {
                    await wowProcess.KeyPress(this.classConfiguration.Blink.ConsoleKey, 120, this.classConfiguration.Blink.Name);
                }
            }

            // should mount
            if (shouldMount && !this.playerReader.PlayerBitValues.IsMounted && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                if (!this.classConfiguration.GatherOnly)
                {
                    shouldMount = false;
                    if (await LookForTarget()) { return; }
                }

                logger.LogInformation("Mounting if level >=40 (druid 30) and no NPC in sight");
                if (!this.npcNameFinder.MobsVisible)
                {
                    if (this.playerReader.PlayerLevel >= 40 && this.playerReader.PlayerClass != PlayerClassEnum.Druid)
                    {
                        await wowProcess.TapStopKey();
                        await Task.Delay(500);
                        await wowProcess.Mount(this.playerReader);
                    }
                    if (this.playerReader.PlayerLevel >= 30 & this.playerReader.PlayerClass == PlayerClassEnum.Druid)
                    {
                        this.classConfiguration.ShapeshiftForm
                          .Where(s => s.ShapeShiftFormEnum == ShapeshiftForm.Druid_Travel)
                          .ToList()
                          .ForEach(async k => await this.wowProcess.KeyPress(k.ConsoleKey, 325));
                    }
                }
                else
                {
                    logger.LogInformation("Not mounting as can see NPC.");
                }
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 3");
            }

            LastActive = DateTime.Now;
        }

        private async Task AdjustHeading(double heading)
        {
            var diff1 = Math.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = Math.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var wanderAngle = 0.3;

            if (!this.classConfiguration.GatherOnly)
            {
                wanderAngle = 0.05;
            }

            if (Math.Min(diff1, diff2) > wanderAngle)
            {
                logger.LogInformation("Correct direction");
                await playerDirection.SetDirection(heading, points.Peek(), "Correcting direction");
            }
            else
            {
                logger.LogInformation($"Direction ok heading: {heading}, player direction {playerReader.Direction}");
            }
        }

        private int PointReachedDistance()
        {
            if (this.playerReader.PlayerClass == PlayerClassEnum.Druid && this.playerReader.Druid_ShapeshiftForm == ShapeshiftForm.Druid_Travel)
            {
                return 50;
            }

            return (this.playerReader.PlayerBitValues.IsMounted ? 50 : 40);
        }

        private async Task<bool> LookForTarget()
        {
            if (this.playerReader.HasTarget && !blacklist.IsTargetBlacklisted())
            {
                return true;
            }
            else
            {
                await this.wowProcess.KeyPress(ConsoleKey.Tab, 300);
                await Task.Delay(300);

                if (!playerReader.HasTarget)
                {
                    await this.npcNameFinder.FindAndClickNpc(0);
                    //await Task.Delay(300);
                }
            }

            if (this.playerReader.HasTarget && !blacklist.IsTargetBlacklisted())
            {
                if (playerReader.PlayerBitValues.IsMounted)
                {
                    await wowProcess.Dismount();
                }
                await this.TapInteractKey("FollowRouteAction 4");
                return true;
            }
            return false;
        }

        public async Task TapInteractKey(string source)
        {
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 99);
            this.classConfiguration.Interact.SetClicked();
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
    }
}