using System;
using System.Collections.Generic;
using System.Text;
using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class GenericCombatAction : CombatActionBase
    {
        private DateTime lastActive = DateTime.Now;

        public GenericCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration, IPlayerDirection direction)
            : base(wowProcess, playerReader, stopMoving, logger, classConfiguration, direction)
        {
        }
        protected override async Task Fight()
        {
            logger.LogInformation("-");
            if ((DateTime.Now - lastActive).TotalSeconds > 5)
            {
                if (this.LastClicked.ContainsKey(ConsoleKey.H))
                {
                    this.LastClicked.Remove(ConsoleKey.H);
                }
            }

            bool pressed = false;
            foreach (var item in this.classConfiguration.Combat.Sequence.Where(i => i != null))
            {
                pressed = await this.CastIfReady(item);
                if (pressed)
                {
                    RaiseEvent(new ActionEvent(GoapKey.shouldloot, true));
                    break;
                }
            }
            if (!pressed)
            {
                await Task.Delay(500);
            }

            this.lastActive = DateTime.Now;
        }

        public override void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.newtarget)
            {
                logger.LogInformation("?Reset cooldowns");

                this.classConfiguration.Combat.Sequence
                    .Where(i => i.ResetOnNewTarget)
                    .ToList()
                    .ForEach(item =>
                    {
                        logger.LogInformation($"Reset cooldown on {item.Name}");
                        if (this.LastClicked.ContainsKey(item.ConsoleKey)) { this.LastClicked.Remove(item.ConsoleKey); }
                    });
            }

            base.OnActionEvent(sender, e);
        }
    }
}
