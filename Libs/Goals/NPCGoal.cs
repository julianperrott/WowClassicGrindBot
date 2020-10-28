using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public abstract class NPCGoal : GoapGoal, IRouteProvider
    {
        private double RADIAN = Math.PI * 2;
        protected readonly WowProcess wowProcess;
        private Stack<WowPoint> routeToWaypoint = new Stack<WowPoint>();

        public List<WowPoint> PathingRoute()
        {
            return routeToWaypoint.ToList();
        }

        public WowPoint? NextPoint()
        {
            return routeToWaypoint.Count == 0 ? null : routeToWaypoint.Peek();
        }

        protected readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly StuckDetector stuckDetector;
        protected readonly ClassConfiguration classConfiguration;
        private readonly IPPather pather;
        protected readonly BagReader bagReader;
        private double lastDistance = 999;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);
        private bool shouldMount = true;
        protected readonly ILogger logger;

        public NPCGoal(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, StopMoving stopMoving, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather, BagReader bagReader)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;

            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            this.pather = pather;
        }

        protected int failedVendorAttempts { get; set; } = 0;

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                shouldMount = true;
                this.routeToWaypoint.Clear();
                failedVendorAttempts = 0;
            }
        }

        public override async Task PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            await Task.Delay(200);

            if (this.playerReader.PlayerBitValues.PlayerInCombat && this.classConfiguration.Mode != Mode.AttendedGather) { return; }

            if ((DateTime.Now - LastActive).TotalSeconds > 10 || routeToWaypoint.Count == 0)
            {
                await FillRouteToDestination();
            }
            else
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "VendorGoal 1");
            }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());

            await AdjustHeading(heading);

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "VendorGoal 2");
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

            lastDistance = distance;

            if (distance < PointReachedDistance())
            {
                logger.LogInformation($"Move to next point");

                ReduceRoute();

                lastDistance = 999;
                if (routeToWaypoint.Count == 0)
                {
                    await this.stopMoving.Stop();
                    distance = WowPoint.DistanceTo(location, this.GetTargetLocation());
                    if (distance>50)
                    {
                        await FillRouteToDestination();
                    }

                    if (routeToWaypoint.Count == 0)
                    {
                        await MoveCloserToTarget(400);
                        await MoveCloserToTarget(100);

                        // we have reached the target location
                        await InteractWithTarget();
                    }
                }

                this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());

                heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Move to next point");

                distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());

                if (this.classConfiguration.Blink.ConsoleKey != 0 && this.playerReader.ManaPercentage > 90 && this.playerReader.PlayerLevel < 40 && distance > 200)
                {
                    await wowProcess.KeyPress(this.classConfiguration.Blink.ConsoleKey, 120, this.classConfiguration.Blink.Name);
                }
            }

            // should mount
            await MountIfRequired();

            LastActive = DateTime.Now;
        }

        protected abstract Task InteractWithTarget();

        private async Task MoveCloserToTarget(int pressDuration)
        {
            var distance = WowPoint.DistanceTo(playerReader.PlayerLocation, this.GetTargetLocation());
            var lastDistance = distance;
            while (distance <= lastDistance && distance > 5)
            {
                logger.LogInformation($"Distance to vendor spot = {distance}");
                lastDistance = distance;
                var heading = DirectionCalculator.CalculateHeading(playerReader.PlayerLocation, this.GetTargetLocation());
                await playerDirection.SetDirection(heading, this.GetTargetLocation(), "Correcting direction", 0);

                await this.wowProcess.KeyPress(ConsoleKey.UpArrow, pressDuration);
                await this.stopMoving.Stop();
                distance = WowPoint.DistanceTo(playerReader.PlayerLocation, this.GetTargetLocation());
            }
        }

        private async Task MountIfRequired()
        {
            if (shouldMount && !this.playerReader.PlayerBitValues.IsMounted && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                shouldMount = false;

                logger.LogInformation("Mounting if level >=40 (druid 30) and no NPC in sight");

                if (this.playerReader.PlayerLevel >= 40 && this.playerReader.PlayerClass != PlayerClassEnum.Druid)
                {
                    await wowProcess.TapStopKey();
                    await Task.Delay(500);
                    await wowProcess.Mount(this.playerReader);
                }
                if (this.playerReader.PlayerLevel >= 30 && this.playerReader.PlayerClass == PlayerClassEnum.Druid)
                {
                    this.classConfiguration.ShapeshiftForm
                      .Where(s => s.ShapeShiftFormEnum == ShapeshiftForm.Druid_Travel)
                      .ToList()
                      .ForEach(async k => await this.wowProcess.KeyPress(k.ConsoleKey, 325));
                }

                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 3");
            }
        }

        private void ReduceRoute()
        {
            if (routeToWaypoint.Any())
            {
                var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
                var distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
                while (distance < PointReachedDistance() && routeToWaypoint.Any())
                {
                    routeToWaypoint.Pop();
                    if (routeToWaypoint.Any())
                    {
                        distance = WowPoint.DistanceTo(location, routeToWaypoint.Peek());
                    }
                }
            }
        }

        private async Task FillRouteToDestination()
        {
            this.routeToWaypoint.Clear();
            WowPoint target = GetTargetLocation();
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var heading = DirectionCalculator.CalculateHeading(location, target);
            await playerDirection.SetDirection(heading, target, "Set Location target").ConfigureAwait(false);

            // create route to vendo
            await this.stopMoving.Stop();
            var path = await this.pather.FindRouteTo(this.playerReader, target);
            path.Reverse();
            path.ForEach(p => this.routeToWaypoint.Push(p));

            this.ReduceRoute();
            if (this.routeToWaypoint.Count == 0)
            {
                this.routeToWaypoint.Push(target);
            }

            this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());
        }

        protected abstract WowPoint GetTargetLocation();

        private async Task AdjustHeading(double heading)
        {
            var diff1 = Math.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = Math.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var wanderAngle = 0.3;

            if (this.classConfiguration.Mode != Mode.AttendedGather)
            {
                wanderAngle = 0.05;
            }

            if (Math.Min(diff1, diff2) > wanderAngle)
            {
                logger.LogInformation("Correct direction");
                await playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Correcting direction");
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

        public async Task Reset()
        {
            await this.stopMoving.Stop();
        }

        public override async Task Abort()
        {
            await this.stopMoving.Stop();
        }
    }
}