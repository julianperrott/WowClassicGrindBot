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

        protected bool IsOnCooldown(ConsoleKey key, int seconds)
        {
            if (!LastClicked.ContainsKey(key))
            {
                //logger.LogInformation("Cooldown not found" + key.ToString());
                return false;
            }

            bool isOnCooldown = (DateTime.Now - LastClicked[key]).TotalSeconds <= seconds;

            if (key != ConsoleKey.H)
            {
                //logger.LogInformation("On cooldown " + key);
            }
            return isOnCooldown;
        }

        protected bool HasEnoughMana(int value)
        {
            return this.playerReader.ManaCurrent >= value;
        }

        protected bool HasEnoughRage(int value)
        {
            return this.playerReader.ManaCurrent >= value;
        }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            await stopMoving.Stop();

            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            await InteractOnUIError();

            await Fight();
        }

        protected virtual async Task InteractOnUIError()
        {
            switch (this.playerReader.LastUIErrorMessage)
            {
                case UI_ERROR.ERR_BADATTACKFACING:
                case UI_ERROR.ERR_SPELL_FAILED_S:
                case UI_ERROR.ERR_SPELL_OUT_OF_RANGE:
                case UI_ERROR.ERR_BADATTACKPOS:
                    logger.LogInformation("Interact due to: this.playerReader.LastUIErrorMessage");
                    await PressKey(ConsoleKey.H);
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

        public async Task PressKey(ConsoleKey key)
        {
            if (lastKeyPressed == ConsoleKey.H)
            {
                var distance = WowPoint.DistanceTo(lastInteractPostion, this.playerReader.PlayerLocation);

                if (distance > 1)
                {
                    logger.LogInformation($"Stop moving: We have moved since the last interact: {distance}");
                    await wowProcess.KeyPress(ConsoleKey.UpArrow, 101);
                    lastInteractPostion = this.playerReader.PlayerLocation;
                    await Task.Delay(300);
                }
            }

            if (key == ConsoleKey.H)
            {
                lastInteractPostion = this.playerReader.PlayerLocation;
            }

            await wowProcess.KeyPress(key, 301);

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
    }
}