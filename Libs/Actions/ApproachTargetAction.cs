using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class ApproachTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private ILogger logger;
        private bool NeedsToReset = true;

        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        private DateTime lastNpcSearch = DateTime.Now;

        private bool debug = true;
        private bool playerWasInCombat = false;

        private Point mouseLocationOfAdd;

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }

        public ApproachTargetAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, NpcNameFinder npcNameFinder, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.npcNameFinder = npcNameFinder;
            this.logger = logger;
            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;

            AddPrecondition(GoapKey.incombatrange, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);

            var rect = wowProcess.GetWindowRect();
            mouseLocationOfAdd = new Point((int)(rect.right / 2f), (int)((rect.bottom / 20) * 13f));
        }

        public override float CostOfPerformingAction { get => 8f; }

        private int SecondsSinceLastFighting => (int)(DateTime.Now - this.lastFighting).TotalSeconds;

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
            }

            if (NeedsToReset)
            {
                this.stuckDetector.ResetStuckParameters();
            }

            var location = playerReader.PlayerLocation;

            if (!playerReader.PlayerBitValues.PlayerInCombat)
            {
                playerWasInCombat = false;
            }
            else
            {
                // we are in combat
                if (!playerWasInCombat && HasPickedUpAnAdd)
                {
                    logger.LogInformation("Looks like we have an add on approach");
                    await this.stopMoving.Stop();
                    await wowProcess.TapStopKey();
                    await wowProcess.KeyPress(ConsoleKey.F3, 400); // clear target
                    return;
                }
                playerWasInCombat = true;
            }

            await this.TapInteractKey("ApproachTargetAction 1");
            await Task.Delay(500);

            var newLocation = playerReader.PlayerLocation;
            if ((location.X == newLocation.X && location.Y == newLocation.Y && SecondsSinceLastFighting > 5) || this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "ApproachTargetAction");
                await Task.Delay(2000);
                await wowProcess.KeyPress(ConsoleKey.Spacebar, 498);
                this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
            }
            await RandomJump();

            int approachSeconds = (int)(this.stuckDetector.actionDurationSeconds);
            if (approachSeconds > 20)
            {
                await this.stuckDetector.Unstick();
                await this.TapInteractKey("ApproachTargetAction unstick");
                await Task.Delay(500);
            }
        }

        private bool HasPickedUpAnAdd
        {
            get
            {
                logger.LogInformation($"Combat={this.playerReader.PlayerBitValues.PlayerInCombat}, Is Target targetting me={this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer}");
                return this.playerReader.PlayerBitValues.PlayerInCombat && !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer;
            }
        }

        private async Task RandomJump()
        {
            if ((DateTime.Now - LastJump).TotalSeconds > 10)
            {
                if (random.Next(1) == 0)
                {
                    await wowProcess.KeyPress(ConsoleKey.Spacebar, 498);
                }
                LastJump = DateTime.Now;
            }
        }

        private DateTime lastFighting = DateTime.Now;

        public override void OnActionEvent(object sender, ActionEvent e)
        {
            if (sender != this)
            {
                NeedsToReset = true;
                if (e.Key == GoapKey.fighting)
                {
                    lastFighting = DateTime.Now;
                }
            }
        }

        public async Task TapInteractKey(string source)
        {
            await this.wowProcess.KeyPress(ConsoleKey.F10, 300);
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 99);
            this.classConfiguration.Interact.SetClicked();
            this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
        }
    }
}