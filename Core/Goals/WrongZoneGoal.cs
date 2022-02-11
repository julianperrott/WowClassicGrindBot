using Core.GOAP;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class WrongZoneGoal : GoapGoal
    {
        private float RADIAN = MathF.PI * 2;
        private ConfigurableInput input;

        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private float lastDistance = 999;
        public DateTime LastActive { get; set; }
        private ILogger logger;

        public WrongZoneGoal(AddonReader addonReader, ConfigurableInput input, IPlayerDirection playerDirection, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration)
        {
            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.input = input;
            this.playerDirection = playerDirection;
            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            AddPrecondition(GoapKey.incombat, false);
        }

        public override bool CheckIfActionCanRun()
        {
            return addonReader.UIMapId.Value == this.classConfiguration.WrongZone.ZoneId;
        }

        public override float CostOfPerformingAction { get => 19f; }

        public override async ValueTask PerformAction()
        {
            var targetLocation = this.classConfiguration.WrongZone.ExitZoneLocation;

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            await Task.Delay(200);
            input.SetKeyState(input.ForwardKey, true, false, "FollowRouteAction 5");

            if (this.playerReader.Bits.PlayerInCombat) { return; }

            if ((DateTime.UtcNow - LastActive).TotalSeconds > 10)
            {
                this.stuckDetector.SetTargetLocation(targetLocation);
            }

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(targetLocation);
            var heading = DirectionCalculator.CalculateHeading(location, targetLocation);

            if (lastDistance < distance)
            {
                playerDirection.SetDirection(heading, targetLocation, "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                input.SetKeyState(input.ForwardKey, true, false, "FollowRouteAction 6");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    this.stuckDetector.Unstick();
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
                var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

                if (MathF.Min(diff1, diff2) > 0.3)
                {
                    playerDirection.SetDirection(heading, targetLocation, "Correcting direction");
                }
            }

            lastDistance = distance;

            LastActive = DateTime.UtcNow;
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.UtcNow - LastActive).TotalSeconds < 2;
        }
    }
}