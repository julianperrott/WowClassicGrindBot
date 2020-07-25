using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class CombatGoal : GoapGoal
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly CastingHandler castingHandler;
        private ILogger logger;
        private DateTime lastActive = DateTime.Now;
        private readonly ClassConfiguration classConfiguration;
        private DateTime lastPulled = DateTime.Now;

        public CombatGoal(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration, CastingHandler castingHandler)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.classConfiguration = classConfiguration;
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
                    SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, true));
                    break;
                }
            }
            if (!pressed)
            {
                await Task.Delay(500);
            }

            this.lastActive = DateTime.Now;
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
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

        protected bool HasPickedUpAnAdd
        {
            get
            {
                logger.LogInformation($"Combat={this.playerReader.PlayerBitValues.PlayerInCombat}, Is Target targetting me={this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer}");
                return this.playerReader.PlayerBitValues.PlayerInCombat &&
                    !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer
                    && this.playerReader.TargetHealthPercentage == 100;
            }
        }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            if (HasPickedUpAnAdd)
            {
                logger.LogInformation($"Add on combat");
                await this.stopMoving.Stop();
                await wowProcess.TapStopKey();
                await wowProcess.KeyPress(ConsoleKey.F3, 300); // clear target
                return;
            }

            if ((DateTime.Now - lastActive).TotalSeconds > 5 && (DateTime.Now - lastPulled).TotalSeconds > 5)
            {
                logger.LogInformation("Interact and stop");
                await this.castingHandler.TapInteractKey("CombatActionBase PerformAction");
                await this.castingHandler.PressKey(ConsoleKey.UpArrow, "", 57);
            }

            await stopMoving.Stop();

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, true));

            await this.castingHandler.InteractOnUIError();

            await Fight();

            lastActive = DateTime.Now;
        }
    }
}