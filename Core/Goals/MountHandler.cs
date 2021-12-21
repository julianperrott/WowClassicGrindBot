using Core.Goals;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly int mountCastTimeMs = 3000;

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

        public async ValueTask MountUp()
        {
            if (playerReader.Level.Value < minLevelToMount)
            {
                return;
            }

            if (playerReader.Class == PlayerClassEnum.Druid)
            {
                classConfig.Form
                    .Where(s => s.FormEnum == Form.Druid_Travel)
                    .ToList()
                    .ForEach(async key => await castingHandler.CastIfReady(key, key.DelayBeforeCast));
            }
            else
            {
                if (playerReader.Bits.IsFalling)
                {
                    (bool notfalling, double fallingElapsedMs) = await wait.InterruptTask(10000, () => !playerReader.Bits.IsFalling);
                    logger.LogInformation($"{GetType().Name}: waited for landing interrupted: {!notfalling} - {fallingElapsedMs}ms");
                }

                await stopMoving.Stop();
                await wait.Update(1);

                await input.TapMount();

                (bool notStartedCasted, double castStartElapsedMs) = await wait.InterruptTask(400, () => playerReader.Bits.IsMounted || playerReader.IsCasting);
                logger.LogInformation($"{GetType().Name}: casting: {!notStartedCasted} | Mounted: {playerReader.Bits.IsMounted} | Delay: {castStartElapsedMs}ms");

                if (!playerReader.Bits.IsMounted)
                {
                    (bool notmounted, double elapsedMs) = await wait.InterruptTask(mountCastTimeMs, () => playerReader.Bits.IsMounted || !playerReader.IsCasting);
                    logger.LogInformation($"{GetType().Name}: interrupted: {!notmounted} | Mounted: {playerReader.Bits.IsMounted} | Delay: {elapsedMs}ms");
                }
            }
        }

        public async ValueTask Dismount()
        {
            if (playerReader.Class == PlayerClassEnum.Druid && playerReader.Form == Form.Druid_Travel)
            {
                classConfig.Form
                    .Where(s => s.FormEnum == Form.Druid_Travel)
                    .ToList()
                    .ForEach(async k => await input.KeyPress(k.ConsoleKey, 50));
            }
            else
            {
                await input.TapDismount();
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
            logger.LogInformation(text);
        }
    }
}
