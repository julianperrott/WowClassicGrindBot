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

        protected Dictionary<ConsoleKey, DateTime> LastClicked = new Dictionary<ConsoleKey, DateTime>();

        public CombatActionBase(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.logger = logger;

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

            if ((DateTime.Now-lastActive).TotalSeconds>5)
            {
                logger.LogInformation("Interact and stop");
               await this.wowProcess.TapInteractKey();
                await this.PressKey(ConsoleKey.UpArrow, 57);
            }

            await stopMoving.Stop();

            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            await InteractOnUIError();

            await Fight();

            lastActive = DateTime.Now;
        }

        public virtual async Task InteractOnUIError()
        {
            switch (this.playerReader.LastUIErrorMessage)
            {
                case UI_ERROR.ERR_BADATTACKFACING:
                case UI_ERROR.ERR_SPELL_FAILED_S:
                case UI_ERROR.ERR_SPELL_OUT_OF_RANGE:
                case UI_ERROR.ERR_BADATTACKPOS:
                    logger.LogInformation("Interact due to: this.playerReader.LastUIErrorMessage");
                    await this.wowProcess.TapInteractKey();
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

        public async Task PressKey(ConsoleKey key, int duration = 300)
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

            await wowProcess.KeyPress(key, duration);

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

        public bool AddsExist { get; set; }

        public async Task<bool> CastIfReady(KeyConfiguration item)
        {
            if (!item.CastIfAddsVisible && AddsExist)
            {
                logger.LogInformation($"-{item.Name}: Adds exist");
                return false;
            }

            if (item.ManaRequirement > this.playerReader.ManaCurrent)
            {
                logger.LogInformation($"-{item.Name}: mana too low");
                return false;
            }
            if (item.CastIfHealthBelowPercentage > 0 && item.CastIfHealthBelowPercentage < this.playerReader.HealthPercent)
            {
                logger.LogInformation($"-{item.Name}: health too high");
                return false;
            }

            var secs = GetCooldownRemaining(item.Key, item.Cooldown);
            if (secs > 0)
            {
                logger.LogInformation($"-{item.Name}: on cooldown, {secs}s left");
                return false;
            }

            if (!CheckBuff(item)) { return false; }

            logger.LogInformation($"+{item.Name} casting.");
            await PressKey(item.Key, item.PressDuration);
            await Task.Delay(1500);

            if (item.HasCastBar)
            {
                for (int i = 0; i < 2000; i += 100)
                {
                    if (!this.playerReader.IsCasting) { break; }
                    await Task.Delay(100);
                }
            }

            return true;
        }

        private bool CheckBuff(KeyConfiguration item)
        {
            if (!string.IsNullOrEmpty(item.Buff))
            {
                if (this.playerReader.GetBuffFunc(item.Name, item.Buff)())
                {
                    logger.LogInformation($"-{item.Name}: already has this buff '{item.Buff}'");
                    return false;
                }
            }

            return true;
        }
    }
}