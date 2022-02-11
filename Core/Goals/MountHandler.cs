using Core.Goals;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
using System.Numerics;

namespace Core
{
    public class MountHandler
    {
        private readonly ILogger logger;
        private readonly ConfigurableInput input;
        private readonly ClassConfiguration classConfig;
        private readonly Wait wait;
        private readonly PlayerReader playerReader;
        private readonly CastingHandler castingHandler;
        private readonly StopMoving stopMoving;

        private readonly int minLevelToMount = 30;
        private readonly int mountCastTimeMs = 5000;
        private readonly int spellQueueWindowMs = 400;
        private readonly int maxFallTimeMs = 10000;
        private readonly int minDistanceToMount = 40;

        public MountHandler(ILogger logger, ConfigurableInput input, ClassConfiguration classConfig, Wait wait, PlayerReader playerReader, CastingHandler castingHandler, StopMoving stopMoving)
        {
            this.logger = logger;
            this.classConfig = classConfig;
            this.input = input;
            this.wait = wait;
            this.playerReader = playerReader;
            this.castingHandler = castingHandler;
            this.stopMoving = stopMoving;
        }

        public bool CanMount()
        {
            return playerReader.Level.Value >= minLevelToMount;
        }

        public void MountUp()
        {
            if (!CanMount())
            {
                return;
            }

            if (playerReader.Class == PlayerClassEnum.Druid)
            {
                int index = classConfig.Form.FindIndex(s => s.FormEnum == Form.Druid_Travel);
                if (index > -1 &&
                    castingHandler.SwitchForm(playerReader.Form, classConfig.Form[index]))
                {
                    return;
                }
            }

            if (playerReader.Bits.IsFalling)
            {
                (bool fallTimeOut, double fallElapsedMs) = wait.Until(maxFallTimeMs, () => !playerReader.Bits.IsFalling);
                Log($"waited for landing interrupted: {!fallTimeOut} - {fallElapsedMs}ms");
            }

            stopMoving.Stop();
            wait.Update(1);

            input.TapMount();

            (bool castStartTimeOut, double castStartElapsedMs) = wait.Until(spellQueueWindowMs, () => playerReader.Bits.IsMounted || playerReader.IsCasting);
            Log($"casting: {!castStartTimeOut} | Mounted: {playerReader.Bits.IsMounted} | Delay: {castStartElapsedMs}ms");

            if (!playerReader.Bits.IsMounted)
            {
                bool hadTarget = playerReader.HasTarget;
                (bool mountTimeOut, double elapsedMs) = wait.Until(mountCastTimeMs, () => playerReader.Bits.IsMounted || !playerReader.IsCasting || playerReader.HasTarget != hadTarget);
                Log($"interrupted: {!mountTimeOut} | Mounted: {playerReader.Bits.IsMounted} | Delay: {elapsedMs}ms");
            }
        }

        public bool ShouldMount(Vector3 target)
        {
            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(target);
            return distance > minDistanceToMount;
        }

        public void Dismount()
        {
            if (playerReader.Form == Form.Druid_Travel)
            {
                int index = classConfig.Form.FindIndex(s => s.FormEnum == Form.Druid_Travel);
                if (index > -1)
                {
                    input.KeyPress(classConfig.Form[index].ConsoleKey, input.defaultKeyPress);
                }
            }
            else
            {
                input.TapDismount();
            }
        }

        public bool IsMounted()
        {
            return playerReader.Class == PlayerClassEnum.Druid &&
                playerReader.Form == Form.Druid_Travel
                ? true
                : playerReader.Bits.IsMounted;
        }

        private void Log(string text)
        {
            logger.LogInformation($"{nameof(MountHandler)}: {text}");
        }
    }
}
