using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class ApproachTargetGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 8f; }

        private ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly WowInput wowInput;

        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly StuckDetector stuckDetector;

        private bool debug = false;

        private Random random = new Random();
        private DateTime LastJump = DateTime.Now;

        private bool NeedsToReset = true;
        private bool playerWasInCombat = false;

        private DateTime lastFighting = DateTime.Now;

        private int SecondsSinceLastFighting => (int)(DateTime.Now - this.lastFighting).TotalSeconds;

        private bool HasPickedUpAnAdd
        {
            get
            {
                return this.playerReader.PlayerBitValues.PlayerInCombat && !this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer;
            }
        }

        public ApproachTargetGoal(ILogger logger, WowProcess wowProcess, WowInput wowInput, PlayerReader playerReader, StopMoving stopMoving,  StuckDetector stuckDetector)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;
            this.wowInput = wowInput;

            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            
            this.stuckDetector = stuckDetector;

            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);
            AddPrecondition(GoapKey.incombatrange, false);

            AddEffect(GoapKey.incombatrange, true);
        }

        public override async Task PerformAction()
        {
            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowInput.Dismount();
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
                    logger.LogInformation("WARN Bodypull -- Looks like we have an add on approach");
                    logger.LogInformation($"Combat={this.playerReader.PlayerBitValues.PlayerInCombat}, Is Target targetting me={this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer}");
                    
                    await this.stopMoving.Stop();
                    await wowInput.TapClearTarget();
                    await wowInput.TapStopKey();

                    if(playerReader.PetHasTarget)
                    {
                        await this.wowInput.TapTargetPet();
                        await this.wowInput.TapTargetOfTarget();
                    }
                }

                playerWasInCombat = true;
            }

            await this.TapInteractKey("ApproachTargetAction 1");
            await this.playerReader.WaitForNUpdate(1);

            var newLocation = playerReader.PlayerLocation;
            if ((location.X == newLocation.X && location.Y == newLocation.Y && SecondsSinceLastFighting > 5) ||
                this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true, false, "ApproachTargetAction");
                await Wait(100);
                await wowInput.TapJump();
                this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
            }

            await RandomJump();

            //
            int approachSeconds = (int)(this.stuckDetector.actionDurationSeconds);
            if (approachSeconds > 20)
            {
                await this.stuckDetector.Unstick();
                await this.TapInteractKey("ApproachTargetAction unstick");
                await Task.Delay(250);
            }

            if(playerReader.WithInCombatRange && (
                playerReader.PlayerClass == PlayerClassEnum.Rogue ||
                playerReader.PlayerClass == PlayerClassEnum.Warrior ||
                playerReader.PlayerClass == PlayerClassEnum.Paladin))
            {
                Log("WithInCombatRange -- Strictly melee -- Wait a moment");
                await stopMoving.Stop();
                await this.TapInteractKey("ApproachTargetAction engage");
                await Task.Delay(200);
            }
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                NeedsToReset = true;
                playerWasInCombat = false;

                if (e.Key == GoapKey.fighting)
                {
                    lastFighting = DateTime.Now;
                }
            }
        }

        private async Task RandomJump()
        {
            if ((DateTime.Now - LastJump).TotalSeconds > 7)
            {
                if (random.Next(1) == 0)
                {
                    await wowInput.TapJump();
                }
                LastJump = DateTime.Now;
            }
        }


        public async Task TapInteractKey(string source)
        {
            await wowInput.TapInteractKey($"Approach target ({source})");
            this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;
            await wowInput.TapStopAttack();
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }

    }
}