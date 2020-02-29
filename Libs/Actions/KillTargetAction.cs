using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Libs.GOAP;
using System.Diagnostics;

namespace Libs.Actions
{
    public class KillTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private ActionBarStatus actionBar = new ActionBarStatus(0);

        private Dictionary<ConsoleKey, DateTime> LastClicked = new Dictionary<ConsoleKey, DateTime>();

        public KillTargetAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.inmeleerange, true);
        }

        public override float CostOfPerformingAction { get => 4f; }

        private bool IsOnCooldown(ConsoleKey key, int seconds)
        {
            if (!LastClicked.ContainsKey(key))
            {
                //Debug.WriteLine("Cooldown not found" + key.ToString());
                return false;
            }

            bool isOnCooldown= (DateTime.Now - LastClicked[key]).TotalSeconds <= seconds;

            if (key != ConsoleKey.H)
            {
                //Debug.WriteLine("On cooldown " + key);
            }
            return isOnCooldown;
        }

        private ConsoleKey Bloodrage => actionBar.HotKey2 ? ConsoleKey.D2 : ConsoleKey.Escape;
        private ConsoleKey Rend => actionBar.HotKey3 && !IsOnCooldown(ConsoleKey.D3,15) ? ConsoleKey.D3 : ConsoleKey.Escape;
        private ConsoleKey HeroicStrike => actionBar.HotKey4 ? ConsoleKey.D4 : ConsoleKey.Escape;
        private ConsoleKey Overpower => actionBar.HotKey5 ? ConsoleKey.D5 : ConsoleKey.Escape;
        private ConsoleKey Battleshout => actionBar.HotKey6 && !IsOnCooldown(ConsoleKey.D6, 120) ? ConsoleKey.D6 : ConsoleKey.Escape;
        private ConsoleKey Approach => !IsOnCooldown(ConsoleKey.H, 5) ? ConsoleKey.H : ConsoleKey.Escape;


        public override async Task PerformAction()
        {
            await stopMoving.Stop();

            this.actionBar = playerReader.ActionBarUseable_73To96;

            var key = new List<ConsoleKey> { Approach, Battleshout, Bloodrage, Overpower, Rend, HeroicStrike }
                .Where(key => key != ConsoleKey.Escape)
                .ToList()
                .FirstOrDefault();

            if (key != 0)
            {
                await PressKey(key);
            }
        }

        private async Task PressKey(ConsoleKey key)
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
