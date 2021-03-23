using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class CastingHandler
    {
        private readonly ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly WowInput wowInput;

        private readonly PlayerReader playerReader;
        
        private ConsoleKey lastKeyPressed = ConsoleKey.Escape;
        private readonly ClassConfiguration classConfig;
        private readonly IPlayerDirection direction;
        private readonly NpcNameFinder npcNameFinder;

        public CastingHandler(ILogger logger, WowProcess wowProcess, WowInput wowInput, PlayerReader playerReader, ClassConfiguration classConfig, IPlayerDirection direction, NpcNameFinder npcNameFinder)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;
            this.wowInput = wowInput;

            this.playerReader = playerReader;
            
            this.classConfig = classConfig;
            this.direction = direction;
            this.npcNameFinder = npcNameFinder;
        }

        protected bool CanRun(KeyAction item)
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

            if(item.School != SchoolMask.None)
            {
                if(classConfig.ImmunityBlacklist.TryGetValue(playerReader.TargetId, out var list))
                {
                    if(list.Contains(item.School))
                    {
                        return false;
                    }
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

        public async Task<bool> CastIfReady(KeyAction item, int sleepBeforeCast = 0)
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
                await wowInput.TapInteractKey("Stop casting shoot");
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
                await playerReader.WaitForNUpdate(1);
                if (!this.playerReader.IsCasting && this.playerReader.HasTarget)
                {
                    await this.InteractOnUIError();
                    item.LogInformation($"Not casting, pressing it again");
                    await PressKey(item.ConsoleKey, item.Name, item.PressDuration);
                    await playerReader.WaitForNUpdate(1);
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
                        await playerReader.WaitForNUpdate(1);
                        break;
                    }

                    // wait after cast if the delay is different to the default value
                    if (item.DelayAfterCast != new KeyAction().DelayAfterCast)
                    {
                        item.LogInformation($" ... delay after cast {item.DelayAfterCast}");

                        if (item.DelayUntilCombat) // stop waiting if the mob is targetting me
                        {
                            var sw = new Stopwatch();
                            sw.Start();
                            while (sw.ElapsedMilliseconds < item.DelayAfterCast)
                            {
                                await Task.Delay(10);
                                if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            await Task.Delay(item.DelayAfterCast);
                        }
                    }
                    
                    await playerReader.WaitForNUpdate(1);
                }
            }
            if (item.StepBackAfterCast > 0)
            {
                await this.wowProcess.KeyPress(ConsoleKey.DownArrow, item.StepBackAfterCast , $"Step back for {item.StepBackAfterCast}ms");
            }
            return true;
        }

        protected async Task<bool> SwitchToCorrectShapeShiftForm(KeyAction item)
        {
            if (this.playerReader.PlayerClass != PlayerClassEnum.Druid || string.IsNullOrEmpty(item.ShapeShiftForm)
                || this.playerReader.Druid_ShapeshiftForm == item.ShapeShiftFormEnum)
            {
                return true;
            }

            var desiredFormKey = this.classConfig.ShapeshiftForm
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

        public async Task PressKey(ConsoleKey key, string description = "", int duration = 50)
        {
            if (lastKeyPressed == classConfig.Interact.ConsoleKey)
            {
                var distance = WowPoint.DistanceTo(classConfig.Interact.LastClickPostion, this.playerReader.PlayerLocation);

                if (distance > 1)
                {
                    logger.LogInformation($"Stop moving: We have moved since the last interact: {distance}");
                    await wowInput.TapStopKey();
                    classConfig.Interact.SetClicked();
                    await playerReader.WaitForNUpdate(1);
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

                    await wowInput.TapStopAttack(this.playerReader.LastUIErrorMessage.ToString());

                    logger.LogInformation($"Interact due to: this.playerReader.LastUIErrorMessage: {this.playerReader.LastUIErrorMessage}");
                    var facing = this.playerReader.Direction;
                    var location = this.playerReader.PlayerLocation;

                    if (this.classConfig.Interact.SecondsSinceLastClick > 4)
                    {
                        await wowInput.TapInteractKey("CombatActionBase InteractOnUIError 1");
                        await Task.Delay(50);
                    }

                    if (lastError == UI_ERROR.ERR_SPELL_FAILED_S)
                    {
                        await wowInput.TapInteractKey("CombatActionBase InteractOnUIError 2");
                        await playerReader.WaitForNUpdate(3);
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