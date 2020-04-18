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
        protected ILogger logger;
        protected readonly CombatActionBase combatAction;
        private DateTime LastInteract = DateTime.Now;

        public PullTargetAction(WowProcess wowProcess, PlayerReader playerReader, NpcNameFinder npcNameFinder, StopMoving stopMoving, ILogger logger, CombatActionBase combatAction, StuckDetector stuckDetector)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.npcNameFinder = npcNameFinder;
            this.stopMoving = stopMoving;
            this.logger = logger;
            this.combatAction = combatAction;
            this.stuckDetector = stuckDetector;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.pulled, false);
            AddPrecondition(GoapKey.withinpullrange, true);
            AddEffect(GoapKey.pulled, true);
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override async Task PerformAction()
        {
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

                if (!this.combatAction.IsOnCooldown(ConsoleKey.Spacebar, 10))
                {
                    await this.combatAction.PressKey(ConsoleKey.Spacebar, 500);
                }

                if (!this.stuckDetector.IsMoving())
                {
                    await this.stuckDetector.Unstick();
                }

                await Interact();
                await Task.Delay(501);
            }
        }

        public abstract bool ShouldStopBeforePull { get; }

        private async Task Interact()
        {
            if ((DateTime.Now - LastInteract).TotalSeconds > 1)
            {
                await this.wowProcess.TapInteractKey();
                this.LastInteract = DateTime.Now;
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
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                if (playerReader.WithInCombatRange) { return; }
            }
        }
    }
}
