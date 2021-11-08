using Core.Goals;
using Core.GOAP;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace Core
{
    public class StuckDetector
    {
        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        
        private readonly StopMoving stopMoving;
        
        private readonly Random random = new Random();
        private readonly IPlayerDirection playerDirection;

        private Vector3 targetLocation;

        private Stopwatch LastReachedDestiationTimer = new Stopwatch();
        private Stopwatch LastUnstickAttemptTimer = new Stopwatch();
        private float previousDistanceToTarget = 99999;
        private DateTime timeOfLastSignificantMovement = DateTime.Now;

        public StuckDetector(ILogger logger, ConfigurableInput input, PlayerReader playerReader, IPlayerDirection playerDirection, StopMoving stopMoving)
        {
            this.logger = logger;
            this.input = input;

            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.playerDirection = playerDirection;

            ResetStuckParameters();
        }

        public void SetTargetLocation(Vector3 targetLocation)
        {
            this.targetLocation = targetLocation;
            ResetStuckParameters();
        }

        public void ResetStuckParameters()
        {
            LastReachedDestiationTimer.Reset();
            LastReachedDestiationTimer.Start();

            LastUnstickAttemptTimer.Reset();
            LastUnstickAttemptTimer.Start();

            previousDistanceToTarget = 99999;
            timeOfLastSignificantMovement = DateTime.Now;

            //logger.LogInformation("ResetStuckParameters()");
        }

        public delegate void ActionEventHandler(object sender, ActionEventArgs e);

        public event ActionEventHandler? ActionEvent;

        public void SendActionEvent(ActionEventArgs e)
        {
            ActionEvent?.Invoke(this, e);
        }

        public int actionDurationSeconds => (int)(LastReachedDestiationTimer.ElapsedMilliseconds / 1000);
        public int unstickSeconds => (int)(LastUnstickAttemptTimer.ElapsedMilliseconds / 1000);

        public async Task Unstick()
        {
            await input.TapJump();

            logger.LogInformation($"Stuck for {actionDurationSeconds}s, last tried to unstick {unstickSeconds}s ago. Unstick seconds={unstickSeconds}.");

            if (actionDurationSeconds > 240)
            {
                // stuck for 4 minutes
                logger.LogInformation("Stuck for 4 minutes");
                SendActionEvent(new ActionEventArgs(GoapKey.abort, true));
                await Task.Delay(120000);
            }

            if (unstickSeconds > 2)
            {
                int actionDuration = (int)(1000 + (((double)actionDurationSeconds * 1000) / 8));

                if (actionDuration > 20000)
                {
                    actionDuration = 20000;
                }

                if (actionDurationSeconds > 10)
                {
                    // back up a bit, added "remove" move forward
                    logger.LogInformation($"Trying to unstick by backing up for {actionDuration}ms");
                    input.SetKeyState(ConsoleKey.DownArrow, true, false, "StuckDetector_back_up");
                    input.SetKeyState(ConsoleKey.UpArrow, false, false, "StuckDetector");
                    await Task.Delay(actionDuration);
                    input.SetKeyState(ConsoleKey.DownArrow, false, false, "StuckDetector");
                }
                this.stopMoving?.Stop();

                // Turn
                var r = random.Next(0, 2);
                var key = r == 0 ? ConsoleKey.A : ConsoleKey.D;
                var turnDuration = random.Next(0, 800) + 200;
                logger.LogInformation($"Trying to unstick by turning for {turnDuration}ms");
                input.SetKeyState(key, true, false, "StuckDetector");
                await Task.Delay(turnDuration);
                input.SetKeyState(key, false, false, "StuckDetector");

                // Move forward
                var strafeDuration = random.Next(0, 2000) + actionDurationSeconds;
                logger.LogInformation($"Trying to unstick by moving forward after turning for {strafeDuration}ms");
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "StuckDetector");
                await Task.Delay(strafeDuration);

                await input.TapJump();

                var heading = DirectionCalculator.CalculateHeading(this.playerReader.PlayerLocation, targetLocation);
                await playerDirection.SetDirection(heading, targetLocation, "Move to next point");

                LastUnstickAttemptTimer.Reset();
                LastUnstickAttemptTimer.Start();
            }
            else
            {
                await input.TapJump();
            }
        }

        internal bool IsGettingCloser()
        {
            float currentDistanceToTarget = playerReader.PlayerLocation.DistanceTo(targetLocation);

            if (currentDistanceToTarget < previousDistanceToTarget - 5)
            {
                ResetStuckParameters();
                previousDistanceToTarget = currentDistanceToTarget;
                return true;
            }

            if (currentDistanceToTarget > previousDistanceToTarget + 5)
            {
                currentDistanceToTarget = previousDistanceToTarget;
            }

            if ((DateTime.Now - timeOfLastSignificantMovement).TotalSeconds > 3)
            {
                logger.LogInformation("We seem to be stuck!");
                return false;
            }

            return true;
        }

        internal bool IsMoving()
        {
            float currentDistanceToTarget = playerReader.PlayerLocation.DistanceTo(targetLocation);

            if (MathF.Abs(currentDistanceToTarget - previousDistanceToTarget) > 1)
            {
                ResetStuckParameters();
                previousDistanceToTarget = currentDistanceToTarget;
                return true;
            }

            if ((DateTime.Now - timeOfLastSignificantMovement).TotalSeconds > 3)
            {
                logger.LogInformation("We seem to be stuck!");
                return false;
            }

            return true;
        }
    }
}