using Libs.GOAP;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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
        private ILogger logger;
        private bool NeedsToReset=true;

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

        public ApproachTargetAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, NpcNameFinder npcNameFinder, ILogger logger, StuckDetector stuckDetector)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.npcNameFinder = npcNameFinder;
            this.logger = logger;
            this.stuckDetector = stuckDetector;

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
            //logger.LogInformation($"ApproachTargetAction: Incombat={playerReader.PlayerBitValues.PlayerInCombat}, WasInCombat={playerWasInCombat}");

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

            await this.wowProcess.TapInteractKey();
            await Task.Delay(500);

            var newLocation = playerReader.PlayerLocation;
            if ((location.X == newLocation.X && location.Y == newLocation.Y && SecondsSinceLastFighting > 5) || this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                await Task.Delay(2000);
                await wowProcess.KeyPress(ConsoleKey.Spacebar, 498);
                this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
            }
            await RandomJump();

            int approachSeconds = (int)(this.stuckDetector.actionDurationSeconds);
            if (approachSeconds > 20)
            {
                await this.stuckDetector.Unstick();
                await this.wowProcess.TapInteractKey();
                await Task.Delay(500);
            }
        }

        bool HasPickedUpAnAdd
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
    }
}