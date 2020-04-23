//using Libs.GOAP;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Libs.Actions
//{
//    public class WarriorCombatAction : CombatActionBase
//    {
//        public WarriorCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger) : base(wowProcess, playerReader, stopMoving, logger)
//        {
//        }

//        private ConsoleKey Bloodrage => actionBar.HotKey2 ? ConsoleKey.D2 : ConsoleKey.Escape;
//        private ConsoleKey Rend => HasEnoughRage(10) &&  actionBar.HotKey3 && !IsOnCooldown(ConsoleKey.D3, 15) ? ConsoleKey.D3 : ConsoleKey.Escape;
//        private ConsoleKey HeroicStrike => HasEnoughRage(12) && actionBar.HotKey4 ? ConsoleKey.D4 : ConsoleKey.Escape;
//        private ConsoleKey Overpower => HasEnoughRage(5) && actionBar.HotKey5 ? ConsoleKey.D5 : ConsoleKey.Escape;
//        private ConsoleKey Battleshout => HasEnoughRage(10) && actionBar.HotKey6 && !IsOnCooldown(ConsoleKey.D6, 120) ? ConsoleKey.D6 : ConsoleKey.Escape;
//        private ConsoleKey Approach => !IsOnCooldown(ConsoleKey.H, 5) ? ConsoleKey.H : ConsoleKey.Escape;

//        protected override async Task Fight()
//        {
//            this.actionBar = playerReader.ActionBarUseable_73To96;

//            var key = new List<ConsoleKey> { Approach, Battleshout, Bloodrage, Overpower, Rend, HeroicStrike }
//                .Where(key => key != ConsoleKey.Escape)
//                .ToList()
//                .FirstOrDefault();

//            if (key != 0)
//            {
//                await PressKey(key);
//                RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
//            }
//        }

//        public override void OnActionEvent(object sender, ActionEvent e)
//        {
//            if (e.Key == GoapKey.newtarget)
//            {
//                ResetRend();
//            }
//        }

//        public void ResetRend()
//        {
//            logger.LogInformation("Rend reset");
//            LastClicked.Remove(ConsoleKey.D3);
//        }
//    }
//}