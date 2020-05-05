using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class CastingHandler
    {
        protected readonly WowProcess wowProcess;
        protected readonly PlayerReader playerReader;
        protected readonly StopMoving stopMoving;
        protected readonly ILogger logger;
        protected ConsoleKey lastKeyPressed = ConsoleKey.Escape;
        protected readonly ClassConfiguration classConfiguration;
        protected readonly IPlayerDirection direction;
        protected readonly NpcNameFinder npcNameFinder;

        public CastingHandler(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration, IPlayerDirection direction, NpcNameFinder npcNameFinder)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.classConfiguration = classConfiguration;
            this.direction = direction;
            this.npcNameFinder = npcNameFinder;
        }

        protected bool CanRun(KeyConfiguration item)
        {
            if (!string.IsNullOrEmpty(item.CastIfAddsVisible))
            {
                var needAdds = bool.Parse(item.CastIfAddsVisible);
                if (needAdds != npcNameFinder.PotentialAddsExist)
                {
                    item.LogInformation($"Only cast if adds exist = {item.CastIfAddsVisible} and it is {npcNameFinder.PotentialAddsExist}");
                    return false;
                }
            }

            return item.CanRun();
        }

        protected async Task PressCastKeyAndWaitForCastToEnd(ConsoleKey key, int maxWaitMs)
        {
            await PressKey(key);
            if (!this.playerReader.IsCasting)
            {
                // try again
                await PressKey(key);
            }

            for (int i = 0; i < maxWaitMs; i += 100)
            {
                if (!this.playerReader.IsCasting)
                {
                    return;
                }
                await Task.Delay(100);
            }
        }

        public async Task<bool> CastIfReady(KeyConfiguration item, GoapAction source, int sleepBeforeCast = 0)
        {
            if (!CanRun(item))
            {
                return false;
            }

            if (item.ConsoleKey == 0)
            {
                return false;
            }

            if (!await SwitchToCorrectShapeShiftForm(item))
            {
                return false;
            }

            if (this.playerReader.IsShooting)
            {
                await TapInteractKey("Stop casting shoot");
                await Task.Delay(1500); // wait for shooting to end
            }

            if (sleepBeforeCast > 0)
            {
                item.LogInformation($" Wait before {sleepBeforeCast}.");
                await Task.Delay(sleepBeforeCast);
            }

            await PressKey(item.ConsoleKey, item.Name, item.PressDuration);

            item.SetClicked();

            if (!item.HasCastBar)
            {
                item.LogInformation($" ... delay after cast {item.DelayAfterCast}");
                await Task.Delay(item.DelayAfterCast);
            }
            else
            {
                await Task.Delay(300);
                if (!this.playerReader.IsCasting && this.playerReader.HasTarget)
                {
                    await this.InteractOnUIError();
                    item.LogInformation($"Not casting, pressing it again");
                    await PressKey(item.ConsoleKey, item.Name, item.PressDuration);
                    await Task.Delay(300);
                    if (!this.playerReader.IsCasting && this.playerReader.HasTarget)
                    {
                        item.LogInformation($"Still not casting !");
                        await this.InteractOnUIError();
                        return false;
                    }
                }

                item.LogInformation(" waiting for cast bar to end.");
                for (int i = 0; i < 15000; i += 100)
                {
                    if (!this.playerReader.IsCasting)
                    {
                        await Task.Delay(100);
                        break;
                    }

                    if (source.GetType() == typeof(PullTargetAction) && this.playerReader.PlayerBitValues.PlayerInCombat && !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer && this.playerReader.IsCasting)
                    {
                        await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 200, "Stop cast as picked up an add, my mob is not targetting me.");
                        await wowProcess.KeyPress(ConsoleKey.F3, 400); // clear target
                        break;
                    }

                    await Task.Delay(100);
                }
            }

            return true;
        }

        protected async Task<bool> SwitchToCorrectShapeShiftForm(KeyConfiguration item)
        {
            if (this.playerReader.PlayerClass != PlayerClassEnum.Druid || string.IsNullOrEmpty(item.ShapeShiftForm)
                || this.playerReader.Druid_ShapeshiftForm == item.ShapeShiftFormEnum)
            {
                return true;
            }

            var desiredFormKey = this.classConfiguration.ShapeshiftForm
                .Where(s => s.ShapeShiftFormEnum == item.ShapeShiftFormEnum)
                .FirstOrDefault();

            if (desiredFormKey == null)
            {
                logger.LogWarning($"Unable to find key in ShapeshiftForm to transform into {item.ShapeShiftFormEnum}");
                return false;
            }

            await this.wowProcess.KeyPress(desiredFormKey.ConsoleKey, 325);

            return this.playerReader.Druid_ShapeshiftForm == item.ShapeShiftFormEnum;
        }

        public async Task TapInteractKey(string source)
        {
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 99);
            this.classConfiguration.Interact.SetClicked();
        }

        public async Task PressKey(ConsoleKey key, string description = "", int duration = 300)
        {
            if (lastKeyPressed == classConfiguration.Interact.ConsoleKey)
            {
                var distance = WowPoint.DistanceTo(classConfiguration.Interact.LastClickPostion, this.playerReader.PlayerLocation);

                if (distance > 1)
                {
                    logger.LogInformation($"Stop moving: We have moved since the last interact: {distance}");
                    await wowProcess.TapStopKey();
                    classConfiguration.Interact.SetClicked();
                    await Task.Delay(300);
                }
            }

            await wowProcess.KeyPress(key, duration, description);

            lastKeyPressed = key;
        }

        public virtual async Task InteractOnUIError()
        {
            var lastError = this.playerReader.LastUIErrorMessage;
            switch (this.playerReader.LastUIErrorMessage)
            {
                case UI_ERROR.ERR_BADATTACKFACING:
                case UI_ERROR.ERR_SPELL_FAILED_S:
                case UI_ERROR.ERR_SPELL_OUT_OF_RANGE:
                case UI_ERROR.ERR_BADATTACKPOS:
                case UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR:

                    await this.wowProcess.KeyPress(ConsoleKey.F10, 500, $"Stop attack {this.playerReader.LastUIErrorMessage}");

                    logger.LogInformation($"Interact due to: this.playerReader.LastUIErrorMessage: {this.playerReader.LastUIErrorMessage}");
                    var facing = this.playerReader.Direction;
                    var location = this.playerReader.PlayerLocation;

                    if (this.classConfiguration.Interact.SecondsSinceLastClick > 4)
                    {
                        await this.TapInteractKey("CombatActionBase InteractOnUIError 1");
                        await Task.Delay(500);
                    }

                    if (lastError == UI_ERROR.ERR_SPELL_FAILED_S)
                    {
                        await this.TapInteractKey("CombatActionBase InteractOnUIError 2");
                        await Task.Delay(1000);
                        if (this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_BADATTACKPOS && this.playerReader.Direction == facing)
                        {
                            logger.LogInformation("Turning 180 as I have not moved!");
                            var desiredDirection = facing + Math.PI;
                            desiredDirection = desiredDirection > Math.PI * 2 ? desiredDirection - Math.PI * 2 : desiredDirection;
                            await this.direction.SetDirection(desiredDirection, new WowPoint(0, 0), "InteractOnUIError");
                        }
                    }

                    this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    break;
            }
        }
    }
}