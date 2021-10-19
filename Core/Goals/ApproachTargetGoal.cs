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
        private readonly StopMoving stopMoving;

        private readonly bool debug = true;

        private readonly Random random = new Random(DateTime.Now.Millisecond);

        private DateTime approachStart;

        private bool playerWasInCombat;
        private double lastPlayerDistance;
        private WowPoint lastPlayerLocation;

        private long initialTargetGuid;
        private double initialMinRange;

        private int SecondsSinceApproachStarted => (int)(DateTime.Now - approachStart).TotalSeconds;

        private bool HasPickedUpAnAdd
        {
            get
            {
                return playerReader.PlayerBitValues.PlayerInCombat && !playerReader.PlayerBitValues.TargetOfTargetIsPlayer;
            }
        }

        public ApproachTargetGoal(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader, StopMoving stopMoving)
        {
            this.logger = logger;
            this.input = input;

            this.wait = wait;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;

            lastPlayerDistance = 0;
            lastPlayerLocation = playerReader.PlayerLocation;

            initialTargetGuid = playerReader.TargetGuid;
            initialMinRange = 0;

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

            initialTargetGuid = playerReader.TargetGuid;
            initialMinRange = playerReader.MinRange;

            approachStart = DateTime.Now;
        }

        public override async Task PerformAction()
        {
            lastPlayerLocation = playerReader.PlayerLocation;
            await wait.Update(1);

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
                    await wait.Update(1);

                    if (playerReader.PetHasTarget)
                    {
                        await input.TapTargetPet();
                        await input.TapTargetOfTarget();
                        await wait.Update(1);
                    }
                }

                playerWasInCombat = true;
            }

            if (input.ClassConfig.Approach.GetCooldownRemaining() == 0)
            {
                await input.TapApproachKey("");
            }

            lastPlayerDistance = WowPoint.DistanceTo(lastPlayerLocation, playerReader.PlayerLocation);

            if (lastPlayerDistance < 0.05 && playerReader.LastUIErrorMessage == UI_ERROR.ERR_AUTOFOLLOW_TOO_FAR)
            {
                playerReader.LastUIErrorMessage = UI_ERROR.NONE;

                input.SetKeyState(ConsoleKey.UpArrow, true, false, $"{GetType().Name}: Too far, start moving forward!");
                await wait.Update(1);
            }

            if (SecondsSinceApproachStarted > 1 && lastPlayerDistance < 0.05 && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                await input.TapClearTarget("");
                await wait.Update(1);
                await input.KeyPress(random.Next(2) == 0 ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow, 1000, $"Seems stuck! Clear Target. Turn away. d: {lastPlayerDistance}");

                approachStart = DateTime.Now;
            }

            if (SecondsSinceApproachStarted > 15 && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                await input.TapClearTarget("");
                await wait.Update(1);
                await input.KeyPress(random.Next(2) == 0 ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow, 1000, "Too long time. Clear Target. Turn away.");

                approachStart = DateTime.Now;
            }

            if (playerReader.TargetGuid == initialTargetGuid)
            {
                var initialTargetMinRange = playerReader.MinRange;
                if (!playerReader.PlayerBitValues.PlayerInCombat)
                {
                    await input.TapNearestTarget("Try to find closer target...");
                    await wait.Update(1);
                }

                if (playerReader.TargetGuid != initialTargetGuid)
                {
                    if (playerReader.HasTarget) // blacklist
                    {
                        if (playerReader.MinRange < initialTargetMinRange)
                        {
                            Log($"Found a closer target! {playerReader.MinRange} < {initialTargetMinRange}");
                            initialMinRange = playerReader.MinRange;
                        }
                        else
                        {
                            initialTargetGuid = -1;
                            await input.TapLastTargetKey($"Stick to initial target!");
                            await wait.Update(1);
                        }
                    }
                    else
                    {
                        Log($"Lost the target due blacklist!");
                    }
                }
            }

            if (initialMinRange < playerReader.MinRange && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log($"We are going away from the target! {initialMinRange} < {playerReader.MinRange}");
                await input.TapClearTarget();
                await wait.Update(1);

                approachStart = DateTime.Now;
            }

            await RandomJump();
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.resume)
            {
                approachStart = DateTime.Now;
            }
        }

        private async Task RandomJump()
        {
            if (input.ClassConfig.Jump.MillisecondsSinceLastClick > random.Next(5000, 7000))
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