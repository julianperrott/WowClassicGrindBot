using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public abstract class CombatActionBase : GoapAction
    {
        protected readonly WowProcess wowProcess;
        protected readonly PlayerReader playerReader;
        protected readonly StopMoving stopMoving;
        protected ILogger logger;
        protected ActionBarStatus actionBar = new ActionBarStatus(0);
        protected ConsoleKey lastKeyPressed = ConsoleKey.Escape;
        protected WowPoint lastInteractPostion = new WowPoint(0, 0);
        private DateTime lastActive = DateTime.Now;
        protected readonly ClassConfiguration classConfiguration;
        protected readonly IPlayerDirection direction;
        protected DateTime lastPulled = DateTime.Now;

        protected Dictionary<ConsoleKey, DateTime> LastClicked = new Dictionary<ConsoleKey, DateTime>();

        public bool AddsExist { get; set; }

        public CombatActionBase(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration, IPlayerDirection direction)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.classConfiguration = classConfiguration;
            this.direction = direction;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.incombatrange, true);
        }

        public override float CostOfPerformingAction { get => 4f; }

        public int GetCooldownRemaining(ConsoleKey key, int seconds)
        {
            if (!LastClicked.ContainsKey(key))
            {
                return 0;
            }

            return seconds - ((int)(DateTime.Now - LastClicked[key]).TotalSeconds);
        }

        public bool IsOnCooldown(ConsoleKey key, int seconds)
        {
            return GetCooldownRemaining(key, seconds) > 0;
        }

        protected bool HasEnoughMana(int value)
        {
            return this.playerReader.ManaCurrent >= value;
        }

        protected bool HasEnoughRage(int value)
        {
            return this.playerReader.ManaCurrent >= value;
        }

        protected bool HasEnoughEnergy(int value)
        {
            return this.playerReader.ManaCurrent >= value;
        }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            if ((DateTime.Now - lastActive).TotalSeconds > 5 && (DateTime.Now - lastPulled).TotalSeconds > 5)
            {
                logger.LogInformation("Interact and stop");
                await this.wowProcess.TapInteractKey("CombatActionBase PerformAction");
                await this.PressKey(ConsoleKey.UpArrow, "", 57);
            }

            await stopMoving.Stop();

            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            await InteractOnUIError();

            await Fight();

            lastActive = DateTime.Now;
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
                    await this.wowProcess.TapInteractKey("CombatActionBase InteractOnUIError 1");
                    await Task.Delay(500);
                    if (lastError == UI_ERROR.ERR_SPELL_FAILED_S)
                    {
                        await this.wowProcess.TapInteractKey("CombatActionBase InteractOnUIError 2");
                        await Task.Delay(1000);
                        if (this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_BADATTACKPOS && this.playerReader.Direction == facing)
                        {
                            logger.LogInformation("Turning 180 as I have not moved!");
                            var desiredDirection = facing + Math.PI;
                            desiredDirection = desiredDirection > Math.PI * 2 ? desiredDirection - Math.PI * 2 : desiredDirection;
                            await this.direction.SetDirection(desiredDirection, new WowPoint(0, 0), "InteractOnUIError");
                        }
                    }

                    //if (lastError == UI_ERROR.ERR_BADATTACKPOS)
                    //{
                    //    await Task.Delay(1000);
                    //    if (this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_BADATTACKPOS 
                    //        && this.playerReader.Direction == facing
                    //        && WowPoint.DistanceTo(this.playerReader.PlayerLocation, location)<0.2
                    //        )
                    //    {
                    //        //var desiredDirection = facing + Math.PI;
                    //        //desiredDirection = desiredDirection > Math.PI * 2 ? desiredDirection - Math.PI * 2 : desiredDirection;
                    //        //await this.direction.SetDirection(desiredDirection, new WowPoint(0, 0), "Reverse facing as interact failed");

                    //        await this.wowProcess.KeyPress(ConsoleKey.F3, 200, "ERR_BADATTACKPOS Stop Combat");
                    //    }
                    //}

                    this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
                    break;
            }
        }

        protected abstract Task Fight();

        public async Task PressCastKeyAndWaitForCastToEnd(ConsoleKey key, int maxWaitMs)
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

        public async Task PressKey(ConsoleKey key, string description = "", int duration = 300)
        {
            if (lastKeyPressed == ConsoleKey.H)
            {
                var distance = WowPoint.DistanceTo(lastInteractPostion, this.playerReader.PlayerLocation);

                if (distance > 1)
                {
                    logger.LogInformation($"Stop moving: We have moved since the last interact: {distance}");
                    await wowProcess.TapStopKey();
                    lastInteractPostion = this.playerReader.PlayerLocation;
                    await Task.Delay(300);
                }
            }

            if (key == ConsoleKey.H)
            {
                lastInteractPostion = this.playerReader.PlayerLocation;
            }

            await wowProcess.KeyPress(key, duration, description);

            lastKeyPressed = key;

            if (LastClicked.ContainsKey(key))
            {
                LastClicked[key] = DateTime.Now;
            }
            else
            {
                LastClicked.Add(key, DateTime.Now);
            }
        }

        private void Log(KeyConfiguration item, string message)
        {
            if (item.Log)
            {
                logger.LogInformation($"{item.Name}: {message}");
            }
        }

        public bool CanRun(KeyConfiguration item)
        {
            if (!string.IsNullOrEmpty(item.CastIfAddsVisible))
            {
                var needAdds = bool.Parse(item.CastIfAddsVisible);
                if (needAdds != AddsExist)
                {
                    Log(item, $"Only cast if adds exist = {item.CastIfAddsVisible} and it is {AddsExist}");
                    return false;
                }
            }

            if (item.MinMana > this.playerReader.ManaCurrent)
            {
                Log(item, $"mana too low: {item.MinMana} > {this.playerReader.ManaCurrent}");
                return false;
            }

            if (item.MinComboPoints > this.playerReader.ComboPoints)
            {
                Log(item, "combo points too low: {item.ComboPointRequirement} > {this.playerReader.ComboPoints}");
                return false;
            }

            var secs = GetCooldownRemaining(item.ConsoleKey, item.Cooldown);
            if (secs > 0)
            {
                Log(item, $"on cooldown, {secs}s left");
                return false;
            }

            return true;
        }

        public async Task<bool> CastIfReady(KeyConfiguration item, int sleepBeforeCast = 0)
        {
            if (!CanRun(item) || MeetsRequirement(item))
            {
                return false;
            }

            if (item.ConsoleKey == 0)
            {
                return false;
            }

            await SwitchToCorrectShapeShiftForm(item);

            if (this.playerReader.IsShooting)
            {
                await PressKey(ConsoleKey.H, "Stop casting shoot", 300);
                await Task.Delay(1300); // wait for shooting to end
            }

            if (sleepBeforeCast > 0)
            {
                Log(item, $" Wait before {sleepBeforeCast}.");
                await Task.Delay(sleepBeforeCast);
            }

            await PressKey(item.ConsoleKey, item.Name, item.PressDuration);

            if (!item.HasCastBar)
            {
                Log(item, $" ... delay after cast {item.DelayAfterCast}");
                await Task.Delay(item.DelayAfterCast);
            }
            else
            {
                await Task.Delay(300);
                if (!this.playerReader.IsCasting)
                {
                    await this.InteractOnUIError();
                    Log(item, $"Not casting, pressing it again");
                    await PressKey(item.ConsoleKey, item.Name, item.PressDuration);
                    await Task.Delay(300);
                    if (!this.playerReader.IsCasting)
                    {
                        Log(item, $"Still not casting !");
                        await this.InteractOnUIError();
                        return false;
                    }
                }

                var inCombat = this.playerReader.PlayerBitValues.PlayerInCombat;

                Log(item, " waiting for cast bar to end.");
                for (int i = 0; i < 2000; i += 100)
                {
                    if (!this.playerReader.IsCasting)
                    {
                        await Task.Delay(100);
                        break;
                    }

                    if (inCombat = false && this.playerReader.PlayerBitValues.PlayerInCombat)
                    {
                        if (!this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                        {
                            await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 100, "Stop cast as picked up an add");
                            await wowProcess.KeyPress(ConsoleKey.F3, 400); // clear target
                            break;
                        }
                    }

                    await Task.Delay(100);
                }
            }

            return true;
        }

        public async Task SwitchToCorrectShapeShiftForm(KeyConfiguration item)
        {
            if (this.playerReader.PlayerClass != PlayerClassEnum.Druid || string.IsNullOrEmpty(item.ShapeShiftForm)
                || this.playerReader.Druid_ShapeshiftForm == item.ShapeShiftFormEnum)
            {
                return;
            }

            var desiredFormKey = this.classConfiguration.ShapeshiftForm
                .Where(s => s.ShapeShiftFormEnum == item.ShapeShiftFormEnum)
                .FirstOrDefault();

            if (desiredFormKey == null)
            {
                logger.LogWarning($"Unable to find key in ShapeshiftForm to transform into {item.ShapeShiftFormEnum}");
                return;
            }

            await this.wowProcess.KeyPress(desiredFormKey.ConsoleKey, 325);
        }

        public bool MeetsRequirement(KeyConfiguration item)
        {
            if (!item.RequirementObjects.Any())
            {
                return false;
            }

            bool meetsRequirement = !item.RequirementObjects.Any(r => !r.HasRequirement());
            Log(item, $"{item.Requirement.ToString()} = {meetsRequirement}");
            return meetsRequirement;
        }

        public override void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.pulled)
            {
                this.lastPulled = DateTime.Now;
            }
        }
    }
}