using System;
using System.Collections.Generic;
using System.Text;
using Libs.GOAP;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public abstract class PullTargetAction : GoapAction
    {
        protected readonly WowProcess wowProcess;
        protected readonly PlayerReader playerReader;
        protected readonly NpcNameFinder npcNameFinder;
        protected readonly StopMoving stopMoving;
        protected readonly StuckDetector stuckDetector;
        protected readonly ClassConfiguration classConfiguration;
        protected ILogger logger;
        protected readonly CombatActionBase combatAction;
        private DateTime PullStartTime = DateTime.Now;
        private DateTime LastActive = DateTime.Now;

        public PullTargetAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving, ILogger logger, CombatActionBase combatAction, StuckDetector stuckDetector, ClassConfiguration classConfiguration)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.npcNameFinder = npcNameFinder;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.combatAction = combatAction;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.pulled, false);
            AddPrecondition(GoapKey.withinpullrange, true);
            AddEffect(GoapKey.pulled, true);
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
            
            RaiseEvent(new ActionEvent(GoapKey.fighting, true));

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

                //if (!this.combatAction.IsOnCooldown(ConsoleKey.Spacebar, 10))
                //{
                //    await this.combatAction.PressKey(ConsoleKey.Spacebar,"", 500);
                //}

                if (!this.stuckDetector.IsMoving())
                {
                    await this.stuckDetector.Unstick();
                }

                await Interact();
                await Task.Delay(501);
            }
            else
            {
                this.RaiseEvent(new ActionEvent(GoapKey.pulled, true));
                this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
            }
        }

        public abstract bool ShouldStopBeforePull { get; }

        private async Task Interact()
        {
            if (this.classConfiguration.Interact.SecondsSinceLastClick > 4)
            {
                await this.combatAction.TapInteractKey("PullTargetAction");
            }

            await this.combatAction.InteractOnUIError();
        }

        protected bool HasPickedUpAnAdd
        {
            get
            {
                logger.LogInformation($"Combat={this.playerReader.PlayerBitValues.PlayerInCombat}, Is Target targetting me={this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer}");
                return this.playerReader.PlayerBitValues.PlayerInCombat && !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer;
            }
        }

        protected Random random = new Random();

        public abstract Task<bool> Pull();

        protected async Task WaitForWithinMelleRange()
        {
            this.logger.LogInformation("Waiting for Mellee range");
            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(100);
                if (playerReader.WithInCombatRange|| (!this.playerReader.PlayerBitValues.PlayerInCombat && i>20)) 
                { 
                    return; 
                }
            }
        }
    }
}
