//using Libs.GOAP;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Libs.Actions
//{
//    public class PriestCombatAction : CombatActionBase
//    {
//        public PriestCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger) : base(wowProcess, playerReader, stopMoving, logger)
//        {
//        }

//        private BuffStatus Buffs => playerReader.Buffs;

//        private bool isTargetDamaged => this.playerReader.TargetHealthPercentage < 90;

//        private ConsoleKey Approach => !IsOnCooldown(ConsoleKey.H, isTargetDamaged?15:5) ? ConsoleKey.H : ConsoleKey.Escape;

//        private ConsoleKey Shield => !IsOnCooldown(ConsoleKey.D3, 10) && playerReader.TargetHealthPercentage > 15 && this.playerReader.HealthPercent < 75 && !Buffs.Shield && HasEnoughMana(100) ? ConsoleKey.D3 : ConsoleKey.Escape;
//        private ConsoleKey Renew => !IsOnCooldown(ConsoleKey.D4, 10) && !Buffs.Renew && this.playerReader.HealthPercent < 75 && HasEnoughMana(100) ? ConsoleKey.D4 : ConsoleKey.Escape;
//        private ConsoleKey Heal => !IsOnCooldown(ConsoleKey.D9, 10) && this.playerReader.HealthPercent < 50 && HasEnoughMana(100) ? ConsoleKey.D9 : ConsoleKey.Escape;
//        private ConsoleKey MindBlast => !IsOnCooldown(ConsoleKey.D5, 12) && playerReader.TargetHealthPercentage > 15 && HasEnoughMana(100) ? ConsoleKey.D5 : ConsoleKey.Escape;
//        private ConsoleKey ShadowWordPain => !IsOnCooldown(ConsoleKey.D6, 24) && playerReader.TargetHealthPercentage>35 && HasEnoughMana(100) ? ConsoleKey.D6 : ConsoleKey.Escape;
//        private ConsoleKey Shoot =>  !this.playerReader.IsShooting ? ConsoleKey.D0 : ConsoleKey.Escape;

//        bool lastIsShooting = false;

//        protected override async Task Fight()
//        {
//            this.actionBar = playerReader.ActionBarUseable_1To24;

//            if (lastIsShooting != this.playerReader.IsShooting)
//            {
//                Debug.WriteLine($"## Is Shooting {this.playerReader.IsShooting }");
//                lastIsShooting = this.playerReader.IsShooting;
//            }

//            // make sure we are in spell range
//            if (playerReader.SpellInRange.Priest_Shoot && Approach != ConsoleKey.Escape)
//            {
//                Debug.WriteLine($"## Approach {isTargetDamaged}");
//                await PressKey(Approach);
//                for (int i = 0; i < 10; i++)
//                {
//                    if (playerReader.SpellInRange.Priest_Shoot)
//                    {
//                        await stopMoving.Stop();
//                        break;
//                    }
//                    await Task.Delay(100);
//                }
//            }

//            var key = GetKey();

//            if (key != 0)
//            {
//                if (key == ConsoleKey.D0 && this.playerReader.IsShooting)
//                {
//                    return;
//                }

//                if (this.playerReader.IsShooting)
//                {
//                    Debug.WriteLine($"## Stop casting shoot {isTargetDamaged}");
//                    await PressKey(ConsoleKey.H);

//                    for (int i = 0; i < 20; i++)
//                    {
//                        if (playerReader.ActionBarUseable_1To24.HotKey10) { break; }
//                        await Task.Delay(100);
//                    }
//                }

//                await Task.Delay(300);

//                await PressKey(key);

//                if (key == ConsoleKey.D5) // Mind blast
//                {
//                    await Task.Delay(1200);
//                }

//                if (key == ConsoleKey.D9) // Heal
//                {
//                    await Task.Delay(3000);
//                }

//                RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
//            }
//        }

//        private ConsoleKey GetKey()
//        {
//            return new List<ConsoleKey> { Shield, Heal, Renew, MindBlast, ShadowWordPain, Shoot }
//                .Where(key => key != ConsoleKey.Escape)
//                .ToList()
//                .FirstOrDefault();
//        }

//        public override void OnActionEvent(object sender, ActionEvent e)
//        {
//            if (e.Key == GoapKey.newtarget)
//            {
//                logger.LogInformation("Rend cooldowns as new target");
//                LastClicked.Remove(ConsoleKey.D5); // MindBlast
//                LastClicked.Remove(ConsoleKey.D6); // SWP
//            }
//        }
//    }
//}