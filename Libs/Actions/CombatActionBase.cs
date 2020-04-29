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

            this.classConfiguration.Combat.Sequence.Where(k=>k!=null).ToList().ForEach(key => this.Keys.Add(key));
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            if ((DateTime.Now - lastActive).TotalSeconds > 5 && (DateTime.Now - lastPulled).TotalSeconds > 5)
            {
                logger.LogInformation("Interact and stop");
                await this.TapInteractKey("CombatActionBase PerformAction");
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
                    await this.TapInteractKey("CombatActionBase InteractOnUIError 1");
                    await Task.Delay(500);
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
            if (lastKeyPressed == classConfiguration.Interact.ConsoleKey)
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

            if (key == classConfiguration.Interact.ConsoleKey)
            {
                lastInteractPostion = this.playerReader.PlayerLocation;
            }

            await wowProcess.KeyPress(key, duration, description);

            lastKeyPressed = key;
        }

        private void Log(KeyConfiguration item, string message)
        {
            if (item.Log)
            {
                logger.LogInformation($"{item.Name}: {message}");
            }
        }

        protected bool CanRun(KeyConfiguration item)
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

            return item.CanRun();
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

            await SwitchToCorrectShapeShiftForm(item);

            if (this.playerReader.IsShooting)
            {
                await TapInteractKey("Stop casting shoot");
                await Task.Delay(1500); // wait for shooting to end
            }

            if (sleepBeforeCast > 0)
            {
                Log(item, $" Wait before {sleepBeforeCast}.");
                await Task.Delay(sleepBeforeCast);
            }

            await PressKey(item.ConsoleKey, item.Name, item.PressDuration);

            item.SetClicked();

            if (!item.HasCastBar)
            {
                Log(item, $" ... delay after cast {item.DelayAfterCast}");
                await Task.Delay(item.DelayAfterCast);
            }
            else
            {
                await Task.Delay(300);
                if (!this.playerReader.IsCasting && this.playerReader.HasTarget)
                {
                    await this.InteractOnUIError();
                    Log(item, $"Not casting, pressing it again");
                    await PressKey(item.ConsoleKey, item.Name, item.PressDuration);
                    await Task.Delay(300);
                    if (!this.playerReader.IsCasting && this.playerReader.HasTarget)
                    {
                        Log(item, $"Still not casting !");
                        await this.InteractOnUIError();
                        return false;
                    }
                }

                Log(item, " waiting for cast bar to end.");
                for (int i = 0; i < 15000; i += 100)
                {
                    if (!this.playerReader.IsCasting)
                    {
                        await Task.Delay(100);
                        break;
                    }

                    if (source.GetType()==typeof(PullTargetAction) && this.playerReader.PlayerBitValues.PlayerInCombat && !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer && this.playerReader.IsCasting)
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

        public async Task TapInteractKey(string source)
        {
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey,99);
            this.classConfiguration.Interact.SetClicked();
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