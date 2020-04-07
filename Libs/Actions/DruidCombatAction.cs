using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class DruidCombatAction : CombatActionBase
    {
        public DruidCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger) : base(wowProcess, playerReader, stopMoving, logger)
        {
        }

        private BuffStatus Buffs => playerReader.Buffs;

        private bool isTargetDamaged => this.playerReader.TargetHealthPercentage < 90;

        private ConsoleKey Approach => !IsOnCooldown(ConsoleKey.H, 2) ? ConsoleKey.H : ConsoleKey.Escape;

        //private ConsoleKey Shield => !IsOnCooldown(ConsoleKey.D3, 10) && playerReader.TargetHealthPercentage > 15 && this.playerReader.HealthPercent < 75 && !Buffs.Shield && HasEnoughMana(100) ? ConsoleKey.D3 : ConsoleKey.Escape;
        //private ConsoleKey Renew => !IsOnCooldown(ConsoleKey.D4, 10) && !Buffs.Renew && this.playerReader.HealthPercent < 75 && HasEnoughMana(100) ? ConsoleKey.D4 : ConsoleKey.Escape;
        private ConsoleKey Heal => !IsOnCooldown(ConsoleKey.D9, 10) && this.playerReader.HealthPercent < 50 ? ConsoleKey.D9 : ConsoleKey.Escape;
        private ConsoleKey Wrath =>  HasEnoughMana(30) ? ConsoleKey.D2 : ConsoleKey.Escape;
        private ConsoleKey Maul => !IsOnCooldown(ConsoleKey.D2, 3)? ConsoleKey.D2 : ConsoleKey.Escape;

        private ConsoleKey Enrage => !IsOnCooldown(ConsoleKey.D3, 15) ? ConsoleKey.D3 : ConsoleKey.Escape;
        private ConsoleKey Bash => !IsOnCooldown(ConsoleKey.D4, 10) ? ConsoleKey.D4 : ConsoleKey.Escape;

        //private ConsoleKey ShadowWordPain => !IsOnCooldown(ConsoleKey.D6, 24) && playerReader.TargetHealthPercentage>35 && HasEnoughMana(100) ? ConsoleKey.D6 : ConsoleKey.Escape;
        private ConsoleKey Shoot =>  !this.playerReader.IsActionBar10Current ? ConsoleKey.D0 : ConsoleKey.Escape;

        protected override async Task Fight()
        {
            this.actionBar = playerReader.ActionBarUseable_1To24;

            //Debug.WriteLine($"## Is IsActionBar10Current {this.playerReader.IsActionBar10Current }");

            var key = GetKey();

            if (key != 0)
            {
                await Task.Delay(300);

                if (key == ConsoleKey.D9 && this.playerReader.ShapeshiftForm != 0) // heal
                {
                    await this.wowProcess.KeyPress(ConsoleKey.F8, 300); // cancelform
                }

                if (key != ConsoleKey.D9 && this.playerReader.ShapeshiftForm == 0) // needs bear form
                {
                    await this.wowProcess.KeyPress(ConsoleKey.D4, 300); // bear form
                }

                await PressKey(key);

                if (key == ConsoleKey.D9) // Heal
                {
                    await Task.Delay(3000);
                }

                RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
            }
        }

        private ConsoleKey GetKey()
        {
            var keys = new List<ConsoleKey> { Approach, Heal, Wrath, Shoot };
            if (this.playerReader.Level > 10)
            {
                keys = new List<ConsoleKey> { Approach, Heal, Enrage, Bash, Maul };
            }

            return keys
                .Where(key => key != ConsoleKey.Escape)
                .ToList()
                .FirstOrDefault();
        }

        public override void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.newtarget)
            {
                logger.LogInformation("Rend cooldowns as new target");
                LastClicked.Remove(ConsoleKey.D5); // MindBlast
                LastClicked.Remove(ConsoleKey.D6); // SWP
            }
        }
    }
}