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
    public class GenericCombatAction: CombatActionBase
    {
        private readonly ClassConfiguration classConfiguration;

        private DateTime lastActive = DateTime.Now;

        public GenericCombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration) 
            : base(wowProcess, playerReader, stopMoving, logger)
        {
            this.classConfiguration = classConfiguration;
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
                //LastClicked.Remove(ConsoleKey.D3);
                //LastClicked.Remove(ConsoleKey.D4);

                //LastClicked[ConsoleKey.D3] = DateTime.Now.AddSeconds(-15);
                //LastClicked[ConsoleKey.D4] = DateTime.Now;
            }
        }
    }
}
