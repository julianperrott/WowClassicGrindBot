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
    public class CombatAction : GoapAction
    {
        protected readonly WowProcess wowProcess;
        protected readonly PlayerReader playerReader;
        protected readonly StopMoving stopMoving;
        protected readonly CastingHandler castingHandler;
        protected ILogger logger;
        protected ActionBarStatus actionBar = new ActionBarStatus(0);
        protected ConsoleKey lastKeyPressed = ConsoleKey.Escape;
        private DateTime lastActive = DateTime.Now;
        protected readonly ClassConfiguration classConfiguration;
        protected readonly IPlayerDirection direction;
        protected DateTime lastPulled = DateTime.Now;

        public CombatAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration, IPlayerDirection direction, CastingHandler castingHandler)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.classConfiguration = classConfiguration;
            this.direction = direction;
            this.castingHandler = castingHandler;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.incombatrange, true);

            this.classConfiguration.Combat.Sequence.Where(k => k != null).ToList().ForEach(key => this.Keys.Add(key));
        }

        protected async Task Fight()
        {
            logger.LogInformation("-");
            if ((DateTime.Now - lastActive).TotalSeconds > 5)
            {
                classConfiguration.Interact.ResetCooldown();
            }

            bool pressed = false;
            foreach (var item in this.Keys)
            {
                pressed = await this.castingHandler.CastIfReady(item, this);
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
                        item.ResetCooldown();
                    });
            }

            if (e.Key == GoapKey.pulled)
            {
                this.lastPulled = DateTime.Now;
            }
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            if ((DateTime.Now - lastActive).TotalSeconds > 5 && (DateTime.Now - lastPulled).TotalSeconds > 5)
            {
                logger.LogInformation("Interact and stop");
                await this.castingHandler.TapInteractKey("CombatActionBase PerformAction");
                await this.castingHandler.PressKey(ConsoleKey.UpArrow, "", 57);
            }

            await stopMoving.Stop();

            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

            await this.castingHandler.InteractOnUIError();

            await Fight();

            lastActive = DateTime.Now;
        }

    }
}
