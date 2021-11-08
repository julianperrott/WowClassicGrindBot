using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedLib.Extensions;

namespace Core.Goals
{
    public partial class CorpseRunGoal : GoapGoal
    {
        private float RADIAN = MathF.PI * 2;
        private ConfigurableInput input;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private float lastDistance = 999;
        private readonly List<Vector3> spiritWalkerPath;
        private readonly StuckDetector stuckDetector;
        private Stack<Vector3> points = new Stack<Vector3>();
        public List<Vector3> Deaths { get; } = new List<Vector3>();

        private ILogger logger;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);

        public CorpseRunGoal(PlayerReader playerReader, ConfigurableInput input, IPlayerDirection playerDirection, List<Vector3> spiritWalker, StopMoving stopMoving, ILogger logger, StuckDetector stuckDetector)
        {
            this.playerReader = playerReader;
            this.input = input;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.spiritWalkerPath = spiritWalker.ToList();
            this.logger = logger;
            this.stuckDetector = stuckDetector;

            AddPrecondition(GoapKey.isdead, true);
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            NeedsToReset = true;
            points.Clear();
        }

        public override float CostOfPerformingAction { get => 1f; }

        private bool NeedsToReset = true;

        public override async Task PerformAction()
        {
            if (!await IsDead()) { return; }

            if (NeedsToReset)
            {
                Reset();
                this.stuckDetector.SetTargetLocation(points.Peek());
            }

            await Task.Delay(200);

            var location = playerReader.PlayerLocation;
            float distance = 0;
            float heading = 0;

            if (points.Count == 0)
            {
                logger.LogInformation("No points left");
                return;
            }
            else
            {
                distance = location.DistanceTo(points.Peek());
                heading = DirectionCalculator.CalculateHeading(location, points.Peek());
            }

            if (lastDistance < distance)
            {
                await playerDirection.SetDirection(heading, points.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                input.SetKeyState(ConsoleKey.UpArrow, true, false, "WalkToCorpseAction");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await stuckDetector.Unstick();
                }
                else
                {
                    await Task.Delay(1000);
                    logger.LogInformation("Resuming movement");
                }
            }
            else // distance closer
            {
                await AdjustHeading(heading);
            }

            lastDistance = distance;

            if (distance < 30 && points.Any())
            {
                logger.LogInformation($"Move to next point");
                points.Pop();
                lastDistance = 999;
                if (points.Count > 0)
                {
                    heading = DirectionCalculator.CalculateHeading(location, points.Peek());
                    await playerDirection.SetDirection(heading, points.Peek(), "Move to next point");

                    this.stuckDetector.SetTargetLocation(points.Peek());
                }
            }

            LastActive = DateTime.Now;
        }

        private async Task AdjustHeading(float heading)
        {
            var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            if (MathF.Min(diff1, diff2) > 0.3)
            {
                await playerDirection.SetDirection(heading, points.Peek(), "Correcting direction");
            }
            else
            {
                logger.LogInformation($"Direction ok heading: {heading}, player direction {playerReader.Direction}");
            }
        }

        private async Task<bool> IsDead()
        {
            bool hadNoHealth = true;
            if (playerReader.HealthPercent > 0)
            {
                NeedsToReset = true;
                await Task.Delay(1000);
                logger.LogInformation("Waiting to die");
                hadNoHealth = false;
            }

            return hadNoHealth && this.playerReader.Bits.DeadStatus;
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }

        public void Reset()
        {
            points.Clear();
            for (int i = spiritWalkerPath.Count - 1; i > -1; i--)
            {
                points.Push(spiritWalkerPath[i]);
            }

            NeedsToReset = false;
        }

        public static bool IsDone()
        {
            return false;
        }
    }
}