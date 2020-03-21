using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PriestCombatAction : CombatActionBase
    {
        public PriestCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger) : base(wowProcess, playerReader, stopMoving, logger)
        {
        }

        private BuffStatus Buffs => playerReader.Buffs;

        private ConsoleKey Approach => !IsOnCooldown(ConsoleKey.H, 4) ? ConsoleKey.H : ConsoleKey.Escape;

        private ConsoleKey Shield => !IsOnCooldown(ConsoleKey.D3, 10) && playerReader.TargetHealthPercentage > 15 && this.playerReader.HealthPercent < 90 && !Buffs.Shield && HasEnoughMana(100) ? ConsoleKey.D3 : ConsoleKey.Escape;
        private ConsoleKey Renew => !IsOnCooldown(ConsoleKey.D4, 10) && !Buffs.Renew && this.playerReader.HealthPercent < 60 && HasEnoughMana(100) ? ConsoleKey.D4 : ConsoleKey.Escape;
        private ConsoleKey Heal => !IsOnCooldown(ConsoleKey.D9, 10) && this.playerReader.HealthPercent < 40 && HasEnoughMana(100) ? ConsoleKey.D9 : ConsoleKey.Escape;
        private ConsoleKey MindBlast => !IsOnCooldown(ConsoleKey.D5, 10) && playerReader.TargetHealthPercentage > 15 && HasEnoughMana(100) ? ConsoleKey.D5 : ConsoleKey.Escape;
        private ConsoleKey ShadowWordPain => !IsOnCooldown(ConsoleKey.D6, 14) && playerReader.TargetHealthPercentage>35 && HasEnoughMana(100) ? ConsoleKey.D6 : ConsoleKey.Escape;
        private ConsoleKey Shoot => !IsOnCooldown(ConsoleKey.D0, 3) && playerReader.ActionBarUseable_1To24.HotKey10 ? ConsoleKey.D0 : ConsoleKey.Escape;

        protected override async Task Fight()
        {
            this.actionBar = playerReader.ActionBarUseable_1To24;

            //this.actionBar.Dump();


            // make sure we are in spell range
            if (playerReader.SpellInRange.Priest_Shoot && Approach != ConsoleKey.Escape)
            {
                await PressKey(Approach);
                for (int i = 0; i < 10; i++)
                {
                    if (playerReader.SpellInRange.Priest_Shoot)
                    {
                        await stopMoving.Stop();
                        break;
                    }
                    await Task.Delay(100);
                }
            }

            var key = new List<ConsoleKey> { Shield, Heal, Renew, MindBlast, ShadowWordPain, Shoot }
                .Where(key => key != ConsoleKey.Escape)
                .ToList()
                .FirstOrDefault();

            if (key != 0)
            {
                if (!playerReader.ActionBarUseable_1To24.HotKey10)
                {
                    await PressKey(ConsoleKey.H);

                    for (int i = 0; i < 10; i++)
                    {
                        if (playerReader.ActionBarUseable_1To24.HotKey10) { break; }
                        await Task.Delay(100);
                    }

                    await Task.Delay(400);
                }

                await PressKey(key);

                if (key == ConsoleKey.D5) // Mind blast
                {
                    await Task.Delay(1000);
                }

                if (key == ConsoleKey.D9) // Heal
                {
                    await Task.Delay(3000);
                }

                RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
            }
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