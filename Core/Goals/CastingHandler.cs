using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class CastingHandler
    {
        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        
        private ConsoleKey lastKeyPressed = ConsoleKey.Escape;
        private readonly ClassConfiguration classConfig;
        private readonly IPlayerDirection direction;
        private readonly NpcNameFinder npcNameFinder;

        public CastingHandler(ILogger logger, ConfigurableInput input, PlayerReader playerReader, ClassConfiguration classConfig, IPlayerDirection direction, NpcNameFinder npcNameFinder)
        {
            this.logger = logger;
            this.input = input;

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
                await input.TapStopAttack("Stop casting Shoot");
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
                var result = await WaitInterrupt(item.DelayAfterCast,
                    () => playerReader.PlayerBitValues.TargetIsDead || playerReader.TargetHealthPercentage < 5);
                item.LogInformation($" ... no castbar delay after cast {result.Item2}ms");
                await InteractOnUIError();
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
                            var result = await WaitInterrupt(item.DelayAfterCast,
                                () => playerReader.PlayerBitValues.TargetIsDead || playerReader.TargetHealthPercentage < 5);
                            item.LogInformation($" ... castbar delay after cast {result.Item2}ms");
                        }
                    }
                    
                    await playerReader.WaitForNUpdate(1);
                }
            }
            if (item.StepBackAfterCast > 0)
            {
                await this.input.KeyPress(ConsoleKey.DownArrow, item.StepBackAfterCast , $"Step back for {item.StepBackAfterCast}ms");
            }

            item.ConsumeCharge();
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

            await this.input.KeyPress(desiredFormKey.ConsoleKey, 325);

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
                    await input.TapStopKey();
                    classConfig.Interact.SetClicked();
                    await playerReader.WaitForNUpdate(1);
                }
            }

            await input.KeyPress(key, duration, description);

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

                    await input.TapStopAttack($"{GetType().Name}: " + this.playerReader.LastUIErrorMessage.ToString());
                    var facing = this.playerReader.Direction;
                    var location = this.playerReader.PlayerLocation;

                    if (this.classConfig.Interact.MillisecondsSinceLastClick > 500)
                    {
                        await input.TapInteractKey($"{GetType().Name} InteractOnUIError by Timer");
                        await Task.Delay(50);
                    }

                    if (lastError == UI_ERROR.ERR_SPELL_FAILED_S)
                    {
                        await input.TapInteractKey($"{GetType().Name} InteractOnUIError by ERR_SPELL_FAILED_S");
                        await playerReader.WaitForNUpdate(1); //3
                        if (this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_BADATTACKPOS && this.playerReader.Direction == facing)
                        {
                            var desiredDirection = facing + Math.PI;
                            desiredDirection = desiredDirection > Math.PI * 2 ? desiredDirection - Math.PI * 2 : desiredDirection;
                            await this.direction.SetDirection(desiredDirection, new WowPoint(0, 0), "InteractOnUIError Turning 180 as I have not moved!");
                        }
                    }

                    this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    break;
            }
        }

        protected async Task<Tuple<bool, int>> WaitInterrupt(int durationMs, Func<bool> exit)
        {
            int elapsedMs = 0;
            while (elapsedMs < durationMs)
            {
                await playerReader.WaitForNUpdate(1);
                elapsedMs += 100;

                if (exit())
                    return Tuple.Create(false, elapsedMs);
            }

            return Tuple.Create(true, elapsedMs);
        }
    }
}