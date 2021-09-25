using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class ApproachTargetGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 8f; }

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly Wait wait;
        private readonly PlayerReader playerReader;
        private readonly ClassConfiguration classConfig;
        private readonly StopMoving stopMoving;

        private readonly bool debug = false;

        private readonly Random random = new Random(DateTime.Now.Millisecond);

        private bool playerWasInCombat;
        private double distance;
        private WowPoint location;
        private DateTime approachStart;

        private int SecondsSinceApproachStarted => (int)(DateTime.Now - approachStart).TotalSeconds;

        private bool HasPickedUpAnAdd
        {
            get
            {
                return playerReader.PlayerBitValues.PlayerInCombat && !playerReader.PlayerBitValues.TargetOfTargetIsPlayer;
            }
        }

        public ApproachTargetGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, ClassConfiguration classConfig, StopMoving stopMoving)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;

            this.classConfig = classConfig;

            distance = 0;
            location = playerReader.PlayerLocation;

            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);
            AddPrecondition(GoapKey.incombatrange, false);

            AddEffect(GoapKey.incombatrange, true);
        }

        public override async Task OnEnter()
        {
            await base.OnEnter();

            if (playerReader.PlayerBitValues.IsMounted)
            {
                await input.TapDismount();
            }

            playerWasInCombat = playerReader.PlayerBitValues.PlayerInCombat;
            approachStart = DateTime.Now;
        }

        public override async Task PerformAction()
        {
            location = playerReader.PlayerLocation;

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
                    logger.LogInformation($"Combat={playerReader.PlayerBitValues.PlayerInCombat}, Is Target targetting me={playerReader.PlayerBitValues.TargetOfTargetIsPlayer}");

                    await stopMoving.Stop();
                    await input.TapClearTarget();

                    if (playerReader.PetHasTarget)
                    {
                        await input.TapTargetPet();
                        await input.TapTargetOfTarget();
                        await wait.Update(1);
                    }
                }

                playerWasInCombat = true;
            }

            await input.TapInteractKey("");
            await wait.Update(1);

            distance = WowPoint.DistanceTo(location, playerReader.PlayerLocation);

            if (distance < 0.5 && playerReader.LastUIErrorMessage == UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR)
            {
                playerReader.LastUIErrorMessage = UI_ERROR.NONE;

                input.SetKeyState(ConsoleKey.UpArrow, true, false, $"{GetType().Name}: Too far, start moving forward!");
                await wait.Update(1);
            }

            if (SecondsSinceApproachStarted > 1 && distance < 0.5)
            {
                await input.TapClearTarget("");
                await wait.Update(1);
                await input.KeyPress(random.Next(2) == 0 ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow, 1000, "Seems stuck! Clear Target. Turn away.");
            }

            if (SecondsSinceApproachStarted > 10)
            {
                await input.TapClearTarget("");
                await wait.Update(1);
                await input.KeyPress(random.Next(2) == 0 ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow, 1000, "Too long time. Clear Target. Turn away.");
            }

            await RandomJump();
        }

        private async Task RandomJump()
        {
            if (classConfig.Jump.MillisecondsSinceLastClick > random.Next(5000, 7000))
            {
                await input.TapJump();
            }
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