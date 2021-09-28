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

        private readonly Wait wait;
        private readonly PlayerReader playerReader;
        
        private ConsoleKey lastKeyPressed = ConsoleKey.Escape;
        private readonly ClassConfiguration classConfig;
        private readonly IPlayerDirection direction;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StopMoving stopMoving;

        private readonly KeyAction defaultKeyAction = new KeyAction();
        private const int MaxWaitCastTimeMs = 500;
        private const int MaxWaitBuffTimeMs = 500;
        private const int MaxCastTimeMs = 15000;
        private const int MaxSwingTimeMs = 4000;
        private const int GCD = 1500;

        public CastingHandler(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, ClassConfiguration classConfig, IPlayerDirection direction, NpcNameFinder npcNameFinder, StopMoving stopMoving)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            
            this.classConfig = classConfig;
            this.direction = direction;
            this.npcNameFinder = npcNameFinder;
            this.stopMoving = stopMoving;
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

        private async Task PressKeyAction(KeyAction item)
        {
            playerReader.LastUIErrorMessage = UI_ERROR.NONE;

            await PressKey(item.ConsoleKey, item.Name + (item.AfterCastWaitNextSwing ? " and wait for next swing!" : ""), item.PressDuration);
            item.SetClicked();
        }

        private async Task<bool> CastInstant(KeyAction item)
        {
            if (item.StopBeforeCast)
            {
                await stopMoving.Stop();
            }

            await PressKeyAction(item);

            (bool input, double inputElapsedMs) = await wait.InterruptTask(item.AfterCastWaitNextSwing ? MaxSwingTimeMs : MaxWaitCastTimeMs,
                () => playerReader.LastUIErrorMessage != UI_ERROR.NONE || !playerReader.CurrentAction.Is(item.Key));
            if (!input)
            {
                item.LogInformation($" ... instant input after {inputElapsedMs}ms");
            }
            else
            {
                item.LogInformation($" ... instant input not registered!");
                return false;
            }

            item.LogInformation($" ... usable: {playerReader.ActionBarUsable.Usable(item.Key)} -- {playerReader.LastUIErrorMessage}");

            if (playerReader.LastUIErrorMessage != UI_ERROR.NONE)
            {
                if (playerReader.LastUIErrorMessage == UI_ERROR.ERR_SPELL_COOLDOWN)
                {
                    item.LogInformation($" ... instant wait until its ready");
                    bool before = playerReader.ActionBarUsable.Usable(item.Key);
                    await wait.While(() => before != playerReader.ActionBarUsable.Usable(item.Key));
                }
                else
                {
                    await ReactToLastUIErrorMessage($"{item.Name}-{GetType().Name}: CastInstant");
                }

                return false;
            }

            return true;
        }

        private async Task<bool> CastCastbar(KeyAction item)
        {
            if (item.StopBeforeCast)
            {
                await stopMoving.Stop();
            }

            bool beforeHasTarget = playerReader.HasTarget;

            await PressKeyAction(item);

            (bool input, double inputElapsedMs) = await wait.InterruptTask(MaxWaitCastTimeMs,
                () => playerReader.IsCasting || playerReader.LastUIErrorMessage != UI_ERROR.NONE);
            if (!input)
            {
                item.LogInformation($" ... castbar input after {inputElapsedMs}ms");
            }
            else
            {
                item.LogInformation($" ... castbar input not registered!");
                return false;
            }

            item.LogInformation($" ... usable: {playerReader.ActionBarUsable.Usable(item.Key)} -- {playerReader.LastUIErrorMessage}");

            if (playerReader.LastUIErrorMessage != UI_ERROR.NONE)
            {
                if (playerReader.LastUIErrorMessage == UI_ERROR.ERR_SPELL_COOLDOWN)
                {
                    item.LogInformation($" ... castbar wait until its ready");
                    bool before = playerReader.ActionBarUsable.Usable(item.Key);
                    await wait.While(() => before != playerReader.ActionBarUsable.Usable(item.Key));
                }
                else
                {
                    await ReactToLastUIErrorMessage($"{item.Name}-{GetType().Name}: CastCastbar");
                }

                return false;
            }

            item.LogInformation(" ... waiting for cast bar to end or target loss.");
            await wait.InterruptTask(MaxCastTimeMs, () => !playerReader.IsCasting || beforeHasTarget != playerReader.HasTarget);

            return true;
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

            if (playerReader.IsShooting)
            {
                await input.TapStopAttack("Stop AutoRepeat Shoot");

                (bool interrupted, double elapsedMs) = await wait.InterruptTask(GCD, 
                    () => playerReader.ActionBarUsable.Usable(item.Key));

                if (!interrupted)
                {
                    item.LogInformation($" ... waited to end Shoot {elapsedMs}ms");
                }
            }

            if (sleepBeforeCast > 0)
            {
                item.LogInformation($" Wait {sleepBeforeCast}ms before press.");
                await Task.Delay(sleepBeforeCast);
            }

            long beforeBuff = playerReader.Buffs.Value;
            bool beforeHasTarget = playerReader.HasTarget;

            (bool gcd, double gcdElapsedMs) = await wait.InterruptTask(GCD,
                () => playerReader.ActionBarUsable.Usable(item.Key) || beforeHasTarget != playerReader.HasTarget);
            if (!gcd)
            {
                item.LogInformation($" ... waited for gcd {gcdElapsedMs}ms");

                if (beforeHasTarget != playerReader.HasTarget)
                {
                    item.LogInformation($" ... lost target!");
                    return false;
                }
            }

            if (!item.HasCastBar)
            {
                if (!await CastInstant(item))
                {
                    // try again after reacted to UI_ERROR
                    if (!await CastInstant(item))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!await CastCastbar(item))
                {
                    // try again after reacted to UI_ERROR
                    if (!await CastCastbar(item))
                    {
                        return false;
                    }
                }
            }

            if (item.AfterCastWaitBuff)
            {
                (bool notappeared, double elapsedMs) = await wait.InterruptTask(MaxWaitBuffTimeMs, () => beforeBuff != playerReader.Buffs.Value);
                logger.LogInformation($" ... AfterCastWaitBuff: Buff: {!notappeared} | Delay: {elapsedMs}ms");
            }

            if (item.DelayAfterCast != defaultKeyAction.DelayAfterCast)
            {
                if (item.DelayUntilCombat) // stop waiting if the mob is targetting me
                {
                    item.LogInformation($" ... DelayUntilCombat ... delay after cast {item.DelayAfterCast}ms");

                    var sw = new Stopwatch();
                    sw.Start();
                    while (sw.ElapsedMilliseconds < item.DelayAfterCast)
                    {
                        await wait.Update(1);
                        if (playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    item.LogInformation($" ... delay after cast {item.DelayAfterCast}ms");
                    var result = await wait.InterruptTask(item.DelayAfterCast, () => beforeHasTarget != playerReader.HasTarget);
                    if (!result.Item1)
                    {
                        item.LogInformation($" .... wait interrupted {result.Item2}ms");
                    }
                }
            }

            if (item.StepBackAfterCast > 0)
            {
                input.SetKeyState(ConsoleKey.DownArrow, true, false, $"Step back for {item.StepBackAfterCast}ms");
                (bool notStepback, double stepbackElapsedMs) = 
                    await wait.InterruptTask(item.StepBackAfterCast, () => beforeHasTarget != playerReader.HasTarget);
                if (!notStepback)
                {
                    item.LogInformation($" .... interrupted stepback | lost target? {beforeHasTarget != playerReader.HasTarget} | {stepbackElapsedMs}ms");
                }
                input.SetKeyState(ConsoleKey.DownArrow, false, false);
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
            await input.KeyPress(key, duration, description);

            lastKeyPressed = key;
        }

        public virtual async Task ReactToLastUIErrorMessage(string source)
        {
            //var lastError = playerReader.LastUIErrorMessage;
            switch (playerReader.LastUIErrorMessage)
            {
                case UI_ERROR.NONE:
                    break;
                case UI_ERROR.ERR_SPELL_OUT_OF_RANGE:
                    logger.LogInformation($"{source} -- React to {UI_ERROR.ERR_SPELL_OUT_OF_RANGE} -- Start moving forward");

                    input.SetKeyState(ConsoleKey.UpArrow, true, false, "");
                    playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    break;
                case UI_ERROR.ERR_BADATTACKFACING:

                    if (playerReader.IsInMeleeRange)
                    {
                        logger.LogInformation($"{source} -- React to {UI_ERROR.ERR_BADATTACKFACING} -- Interact!");
                        await input.TapInteractKey("");
                    }
                    else
                    {
                        logger.LogInformation($"{source} -- React to {UI_ERROR.ERR_BADATTACKFACING} -- Turning 180!");

                        double desiredDirection = playerReader.Direction + Math.PI;
                        desiredDirection = desiredDirection > Math.PI * 2 ? desiredDirection - (Math.PI * 2) : desiredDirection;
                        await direction.SetDirection(desiredDirection, new WowPoint(0, 0), "");
                    }

                    playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    break;
                case UI_ERROR.SPELL_FAILED_MOVING:
                    logger.LogInformation($"{source} -- React to {UI_ERROR.SPELL_FAILED_MOVING} -- Stop moving!");

                    await stopMoving.Stop();
                    await wait.Update(1);
                    playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    break;
                case UI_ERROR.ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS:
                    logger.LogInformation($"{source} -- React to {UI_ERROR.ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS} -- Wait till casting!");
                    await wait.While(() => playerReader.IsCasting);
                    break;
                case UI_ERROR.ERR_SPELL_COOLDOWN:
                    logger.LogInformation($"{source} -- Cant react to {UI_ERROR.ERR_SPELL_FAILED_ANOTHER_IN_PROGRESS}");
                    break;
                case UI_ERROR.ERR_BADATTACKPOS:
                    if (playerReader.IsAutoAttacking)
                    {
                        logger.LogInformation($"{source} -- React to {UI_ERROR.ERR_BADATTACKPOS} -- Interact!");
                        await input.TapInteractKey("");
                        playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    }
                    else
                    {
                        logger.LogInformation($"{source} -- Didn't know how to React to {playerReader.LastUIErrorMessage}");
                    }
                    break;
                default:
                    logger.LogInformation($"{source} -- Didn't know how to React to {playerReader.LastUIErrorMessage}");
                    break;
                //case UI_ERROR.ERR_SPELL_FAILED_S:
                //case UI_ERROR.ERR_BADATTACKPOS:
                //case UI_ERROR.ERR_SPELL_OUT_OF_RANGE:
                //case UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR:
                //    this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                //    break;
            }
        }
    }
}