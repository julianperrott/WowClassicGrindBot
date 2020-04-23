//using Libs.GOAP;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Libs.Actions
//{
//    public class RogueCombatAction : CombatActionBase
//    {
//        public RogueCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger) : base(wowProcess, playerReader, stopMoving, logger)
//        {
//        }


//        private ConsoleKey ColdBlood => !IsOnCooldown(ConsoleKey.D1, 60) ? ConsoleKey.D1 : ConsoleKey.Escape;
//        private ConsoleKey Approach => !IsOnCooldown(ConsoleKey.H, 5) ? ConsoleKey.H : ConsoleKey.Escape;
//        private ConsoleKey SinisterStrike => HasEnoughEnergy(40) ? ConsoleKey.D2 : ConsoleKey.Escape;
//        private ConsoleKey SliceAndDice => HasEnoughEnergy(25) && !IsOnCooldown(ConsoleKey.D3, 20) ? ConsoleKey.D3 : ConsoleKey.Escape;
//        private ConsoleKey Eviscerate => HasEnoughEnergy(35) && !IsOnCooldown(ConsoleKey.D4, 10)? ConsoleKey.D4 : ConsoleKey.Escape;
//        private ConsoleKey Evasion => ConsoleKey.D5;
//        private ConsoleKey Vanish => ConsoleKey.D8;

//        protected override async Task Fight()
//        {
//            this.actionBar = playerReader.ActionBarUseable_1To24;

//            if (playerReader.HealthPercent < 50 && !IsOnCooldown(Evasion, 600))
//            {
//                await PressKey(Evasion);
//            }

//            if (playerReader.HealthPercent < 4 && !IsOnCooldown(Vanish, 600))
//            {
//                await DoVanish();
//                return;
//            }

//            var key = new List<ConsoleKey> { ColdBlood, Approach, SliceAndDice, Eviscerate, SinisterStrike }
//                .Where(key => key != ConsoleKey.Escape)
//                .ToList()
//                .FirstOrDefault();

//            if (key != 0)
//            {
//                await PressKey(key);
//                RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
//            }
//        }

//        private async Task DoVanish()
//        {
//            await PressKey(Vanish);

//            await  wowProcess.KeyPress(ConsoleKey.F3, 400); //clear target

//            //wowProcess.SetKeyState(ConsoleKey.DownArrow,true); //walk backwards

//            for (int i = 0; i < 30; i++)
//            {
//                await Task.Delay(1000);
//                if (playerReader.PlayerBitValues.PlayerInCombat || playerReader.HealthPercent > 60)
//                {
//                    //wowProcess.SetKeyState(ConsoleKey.DownArrow, false);
//                    return;
//                }
//                if (i == 6)
//                {
//                    //wowProcess.SetKeyState(ConsoleKey.DownArrow, false);
//                }
//            }
//           // wowProcess.SetKeyState(ConsoleKey.DownArrow, false);
//            return;
//        }

//        public override void OnActionEvent(object sender, ActionEvent e)
//        {
//            if (e.Key == GoapKey.newtarget)
//            {
//                logger.LogInformation("Rend cooldowns as new target");
//                LastClicked.Remove(ConsoleKey.D3);
//                LastClicked.Remove(ConsoleKey.D4);

//                LastClicked[ConsoleKey.D3] = DateTime.Now.AddSeconds(-15);
//                LastClicked[ConsoleKey.D4] = DateTime.Now;
//            }
//        }
//    }
//}