using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class PullTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StopMoving stopMoving;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private ILogger logger;
        private readonly CastingHandler castingHandler;
        private DateTime PullStartTime = DateTime.Now;
        private DateTime LastActive = DateTime.Now;

        public PullTargetAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving, ILogger logger, CastingHandler castingHandler, StuckDetector stuckDetector, ClassConfiguration classConfiguration)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.npcNameFinder = npcNameFinder;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.castingHandler = castingHandler;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.pulled, false);
            AddPrecondition(GoapKey.withinpullrange, true);
            AddEffect(GoapKey.pulled, true);

            this.classConfiguration.Pull.Sequence.Where(k => k != null).ToList().ForEach(key => this.Keys.Add(key));
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override async Task PerformAction()
        {
            await this.wowProcess.KeyPress(ConsoleKey.F10, 300);
            this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;

            if ((DateTime.Now - LastActive).TotalSeconds > 5)
            {
                PullStartTime = DateTime.Now;
            }
            LastActive = DateTime.Now;

            if ((DateTime.Now - PullStartTime).TotalSeconds > 30)
            {
                await wowProcess.KeyPress(ConsoleKey.F3, 300); // clear target
                await this.wowProcess.KeyPress(ConsoleKey.RightArrow, 1000, "Turn after pull timeout");
                return;
            }

            SendActionEvent(new ActionEventArgs(GoapKey.fighting, true));

            if (playerReader.PlayerBitValues.IsMounted)
            {
                logger.LogInformation($"Dismount");
                await wowProcess.Dismount();
            }

            if (ShouldStopBeforePull)
            {
                logger.LogInformation($"Stop approach");
                await this.stopMoving.Stop();
                await Interact();
                await wowProcess.TapStopKey();
            }

            bool pulled = await Pull();
            if (!pulled)
            {
                if (HasPickedUpAnAdd)
                {
                    logger.LogInformation($"Add on approach");
                    await this.stopMoving.Stop();
                    await wowProcess.TapStopKey();
                    await wowProcess.KeyPress(ConsoleKey.F3, 300); // clear target
                    return;
                }

                if (!this.stuckDetector.IsMoving())
                {
                    await this.stuckDetector.Unstick();
                }

                await Interact();
                await Task.Delay(501);
            }
            else
            {
                this.SendActionEvent(new ActionEventArgs(GoapKey.pulled, true));
                this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
            }
        }

        private async Task Interact()
        {
            if (this.classConfiguration.Interact.SecondsSinceLastClick > 4)
            {
                await this.castingHandler.TapInteractKey("PullTargetAction");
            }

            await this.castingHandler.InteractOnUIError();
        }

        protected bool HasPickedUpAnAdd
        {
            get
            {
                logger.LogInformation($"Combat={this.playerReader.PlayerBitValues.PlayerInCombat}, Is Target targetting me={this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer}");
                return this.playerReader.PlayerBitValues.PlayerInCombat && !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer && this.playerReader.HealthPercent > 98;
            }
        }

        protected async Task WaitForWithinMelleRange()
        {
            this.logger.LogInformation("Waiting for Mellee range");
            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(100);
                if (playerReader.WithInCombatRange || (!this.playerReader.PlayerBitValues.PlayerInCombat && i > 20))
                {
                    return;
                }
            }
        }

        public bool ShouldStopBeforePull => this.classConfiguration.Pull.Sequence.Count > 0;

        public async Task<bool> Pull()
        {
            bool hasCast = false;

            //stop combat
            await this.wowProcess.KeyPress(ConsoleKey.F10, 300);
            this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;

            foreach (var item in this.Keys)
            {
                var sleepBeforeFirstCast = item.StopBeforeCast && !hasCast && 500 > item.DelayBeforeCast ? 500 : item.DelayBeforeCast;

                var success = await this.castingHandler.CastIfReady(item, this, sleepBeforeFirstCast);
                hasCast = hasCast || success;

                if (!this.playerReader.HasTarget)
                {
                    return false;
                }

                if (hasCast && item.WaitForWithinMelleRange)
                {
                    await this.WaitForWithinMelleRange();
                }
            }

            // Wait for combat
            if (hasCast)
            {
                for (int i = 0; i < 40; i++)
                {
                    // wait for combat, for mob to be targetting me or have suffered damage or 2 seconds to have elapsed.
                    // sometimes after casting a ranged attack, we can be in combat before the attack has landed.
                    if (this.playerReader.PlayerBitValues.PlayerInCombat &&
                        (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer || this.playerReader.TargetHealthPercentage < 99 || i > 20))
                    {
                        return true;
                    }

                    await Task.Delay(100);
                }
            }

            return this.playerReader.PlayerBitValues.PlayerInCombat;
        }
    }
}