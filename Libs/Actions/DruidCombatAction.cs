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
        private ConsoleKey Wrath => this.playerReader.HealthPercent >49 && HasEnoughMana(30) ? ConsoleKey.D2 : ConsoleKey.Escape;

        private ConsoleKey Maul => HasEnoughMana(10) && !IsOnCooldown(ConsoleKey.D2, 3) ? ConsoleKey.D2 : ConsoleKey.Escape;
        private ConsoleKey Roar => HasEnoughMana(10) && !IsOnCooldown(ConsoleKey.D5, 30) ? ConsoleKey.D5 : ConsoleKey.Escape;

        private ConsoleKey FaerieFire => !IsOnCooldown(ConsoleKey.D7, 40) ? ConsoleKey.D7 : ConsoleKey.Escape;

        private ConsoleKey Enrage => !IsOnCooldown(ConsoleKey.D3, 15) ? ConsoleKey.D3 : ConsoleKey.Escape;
        private ConsoleKey Bash => HasEnoughMana(10) && !IsOnCooldown(ConsoleKey.D4, 60) ? ConsoleKey.D4 : ConsoleKey.Escape;

        //private ConsoleKey ShadowWordPain => !IsOnCooldown(ConsoleKey.D6, 24) && playerReader.TargetHealthPercentage>35 && HasEnoughMana(100) ? ConsoleKey.D6 : ConsoleKey.Escape;
        private ConsoleKey Shoot => !this.playerReader.IsActionBar10Current ? ConsoleKey.D0 : ConsoleKey.Escape;

        protected override async Task Fight()
        {
            this.actionBar = playerReader.ActionBarUseable_1To24;

            //Debug.WriteLine($"## Is IsActionBar10Current {this.playerReader.IsActionBar10Current }");

            var key = GetKey();

            if (key != 0)
            {
                await Task.Delay(300);

                if (key == ConsoleKey.D9) // heal
                {
                    await UseShapeshiftForm(ShapeshiftForm.None);
                }

                if (key != ConsoleKey.D9) // needs bear form
                {
                    await UseShapeshiftForm(ShapeshiftForm.Druid_Bear);
                }

                if (key == ConsoleKey.D9)
                {
                    await PressCastKeyAndWaitForCastToEnd(key, 3000);
                }
                else
                {
                    await PressKey(key);
                }

                RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
            }
        }

        private static Dictionary<ShapeshiftForm, ConsoleKey> formKeys = new Dictionary<ShapeshiftForm, ConsoleKey>
        {
            { ShapeshiftForm.None, ConsoleKey.F8},
            { ShapeshiftForm.Druid_Bear, ConsoleKey.D4},
            { ShapeshiftForm.Druid_Travel, ConsoleKey.D6},
        };

        public async Task UseShapeshiftForm(ShapeshiftForm desiredForm)
        {
            if (this.playerReader.Druid_ShapeshiftForm != desiredForm)
            {
                await this.wowProcess.KeyPress(formKeys[desiredForm], 325); // desiredFrom
            }
        }

        private ConsoleKey GetKey()
        {
            var keys = new List<ConsoleKey> { Approach, Heal, Wrath, Shoot };
            if (this.playerReader.PlayerLevel > 10)
            {
                keys = new List<ConsoleKey> { Approach, Heal, Enrage, FaerieFire, Roar, Bash, Maul };
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
                logger.LogInformation("new target");

                if (LastClicked.ContainsKey(ConsoleKey.D7))
                {
                    LastClicked.Remove(ConsoleKey.D7);
                }
                if (LastClicked.ContainsKey(ConsoleKey.D5))
                {
                    LastClicked.Remove(ConsoleKey.D5);
                }
            }
            if (e.Key == GoapKey.postloot)
            {
                UseTravelForm().Wait();
            }
        }

        private async Task UseTravelForm()
        {
            if (this.playerReader.HealthPercent < 80)
            {
                await this.stopMoving.Stop();
                await UseShapeshiftForm(ShapeshiftForm.None);
                await PressKey(ConsoleKey.D9);
                await Task.Delay(3000);
            }

            await UseShapeshiftForm(ShapeshiftForm.Druid_Travel);
        }
    }
}