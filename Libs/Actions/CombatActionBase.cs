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

        protected bool HasEnoughMana(int mana)
        {
            return this.playerReader.ManaCurrent > mana;
        }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            await stopMoving.Stop();

            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            await Fight();
        }

        protected abstract Task Fight();

        public async Task PressKey(ConsoleKey key)
        {
            await wowProcess.KeyPress(key, 501);

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