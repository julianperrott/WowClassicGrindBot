using Libs.GOAP;
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
        protected ActionBarStatus actionBar = new ActionBarStatus(0);

        protected Dictionary<ConsoleKey, DateTime> LastClicked = new Dictionary<ConsoleKey, DateTime>();

        public CombatActionBase(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.inmeleerange, true);
        }

        public override float CostOfPerformingAction { get => 4f; }

        protected bool IsOnCooldown(ConsoleKey key, int seconds)
        {
            if (!LastClicked.ContainsKey(key))
            {
                //Debug.WriteLine("Cooldown not found" + key.ToString());
                return false;
            }

            bool isOnCooldown = (DateTime.Now - LastClicked[key]).TotalSeconds <= seconds;

            if (key != ConsoleKey.H)
            {
                //Debug.WriteLine("On cooldown " + key);
            }
            return isOnCooldown;
        }


        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Mount();
            }

            await stopMoving.Stop();

            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            await Fight();
        }

        protected abstract Task Fight();

        protected async Task PressKey(ConsoleKey key)
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