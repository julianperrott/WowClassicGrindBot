using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class WrongZoneGoal : GoapGoal
    {
        private double RADIAN = Math.PI * 2;
        private ConfigurableInput input;

        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private double lastDistance = 999;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);
        private ILogger logger;

        public WrongZoneGoal(PlayerReader playerReader, ConfigurableInput input, IPlayerDirection playerDirection, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration)
        {
            this.playerReader = playerReader;
            this.input = input;
            this.playerDirection = playerDirection;
            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            AddPrecondition(GoapKey.incombat, false);
        }

        public override bool CheckIfActionCanRun()
        {
            return this.playerReader.UIMapId.Value == this.classConfiguration.WrongZone.ZoneId;
        }

        public override float CostOfPerformingAction { get => 19f; }

        public override async Task PerformAction()
        {
            var targetLocation = this.classConfiguration.WrongZone.ExitZoneLocation;

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            await Task.Delay(200);
            input.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 5");

            if (this.playerReader.Bits.PlayerInCombat) { return; }

            if ((DateTime.Now - LastActive).TotalSeconds > 10)
            {
                this.stuckDetector.SetTargetLocation(targetLocation);
            }

            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, targetLocation);
            var heading = DirectionCalculator.CalculateHeading(location, targetLocation);

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, targetLocation, "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "FollowRouteAction 6");
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
                var diff1 = Math.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
                var diff2 = Math.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

                if (Math.Min(diff1, diff2) > 0.3)
                {
                    await playerDirection.SetDirection(heading, targetLocation, "Correcting direction");
                }
            }

            lastDistance = distance;

            LastActive = DateTime.Now;
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }
    }
}