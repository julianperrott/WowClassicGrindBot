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
        private ILogger logger;

        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        private DateTime lastNpcSearch = DateTime.Now;

        private bool debug = true;
        private bool playerWasInCombat = false;

        private Point mouseLocationOfAdd;
        private Stopwatch timeApproachingtarget = new Stopwatch();
        private Stopwatch LastUnstickAttempt = new Stopwatch();

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }

        public ApproachTargetAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, NpcNameFinder npcNameFinder, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.npcNameFinder = npcNameFinder;
            this.logger = logger;

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
            if (!timeApproachingtarget.IsRunning) { timeApproachingtarget.Start(); }
            if (!LastUnstickAttempt.IsRunning) { LastUnstickAttempt.Start(); }

            //logger.LogInformation($"ApproachTargetAction: Incombat={playerReader.PlayerBitValues.PlayerInCombat}, WasInCombat={playerWasInCombat}");

            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Dismount();
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
                    await this.wowProcess.KeyPress(ConsoleKey.UpArrow, 490);
                    await wowProcess.KeyPress(ConsoleKey.F3, 400); // clear target
                    return;
                }
                playerWasInCombat = true;
            }

            await this.wowProcess.KeyPress(ConsoleKey.H, 501);

            var newLocation = playerReader.PlayerLocation;
            if (location.X == newLocation.X && location.Y == newLocation.Y && SecondsSinceLastFighting > 5)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                await Task.Delay(2000);
                await wowProcess.KeyPress(ConsoleKey.Spacebar, 498);
            }
            await RandomJump();

            int approachSeconds = (int)(timeApproachingtarget.ElapsedMilliseconds / 1000);
            if (approachSeconds > 20)
            {
                await Unstick();
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

        private async Task Unstick()
        {
            await wowProcess.KeyPress(ConsoleKey.Spacebar, 500);

            int approachSeconds = (int)(timeApproachingtarget.ElapsedMilliseconds / 1000);
            int unstickSeconds = (int)(LastUnstickAttempt.ElapsedMilliseconds / 1000);

            logger.LogInformation($"Stuck for {approachSeconds}s, last tried to unstick {unstickSeconds}s ago");

            if (approachSeconds > 240)
            {
                // stuck for 4 minutes
                logger.LogInformation("Stuck for 4 minutes on approach");
                RaiseEvent(new ActionEvent(GoapKey.abort, true));
            }

            if (unstickSeconds > 10)
            {
                this.stopMoving?.Stop();
                // stuck for 30 seconds
                logger.LogInformation("Trying to unstick by strafing");
                var r = random.Next(0, 100);
                if (r < 50)
                {
                    wowProcess.SetKeyState(ConsoleKey.Q, true);
                    await Task.Delay(5 * 1000);
                    wowProcess.SetKeyState(ConsoleKey.Q, false);
                }
                else
                {
                    wowProcess.SetKeyState(ConsoleKey.E, true);
                    await Task.Delay(5 * 1000);
                    wowProcess.SetKeyState(ConsoleKey.E, false);
                }
                LastUnstickAttempt.Reset(); 
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
                if (e.Key == GoapKey.fighting)
                {
                    lastFighting = DateTime.Now;
                    timeApproachingtarget.Reset();
                    LastUnstickAttempt.Reset();
                }
            }
        }



    }
}