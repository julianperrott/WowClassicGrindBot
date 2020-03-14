using Libs.Cursor;
using Libs.GOAP;
using Libs.NpcFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class ApproachTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly NpcNameFinder npcNameFinder;

        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        private DateTime lastNpcSearch = DateTime.Now;

        private bool debug=true;

        private Point mouseLocationOfAdd;

        private void Log(string text)
        {
            if (debug)
            {
                Debug.WriteLine($"{this.GetType().Name}: {text}");
            }
        }

        public ApproachTargetAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, NpcNameFinder npcNameFinder)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.npcNameFinder = npcNameFinder;

            AddPrecondition(GoapKey.inmeleerange, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);

            var rect = wowProcess.GetWindowRect();
            mouseLocationOfAdd = new Point((int)(rect.right / 2f), (int)((rect.bottom / 20) * 13f));
        }

        public override float CostOfPerformingAction { get => 8f; }

        private int SecondsSinceLastFighting => (int)(DateTime.Now - this.lastFighting).TotalSeconds;

        public override async Task PerformAction()
        {
            var location = playerReader.PlayerLocation;

            if (SecondsSinceLastFighting > 10)
            {
                await CheckForNpcFollowingMe();
            }

            if (playerReader.PlayerBitValues.IsMounted)
            {
                await wowProcess.Mount();
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
        }

        private async Task CheckForNpcFollowingMe()
        {
            wowProcess.SetCursorPosition(mouseLocationOfAdd);
            CursorClassifier.Classify(out var cls);
            if (cls == CursorClassification.Kill)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                Log("We are being attacked, switching target");
                await wowProcess.LeftClickMouse(mouseLocationOfAdd);
                await Task.Delay(1500);
            }
        }

        private async Task RandomJump()
        {
            if ((DateTime.Now - LastJump).TotalSeconds > 10)
            {
                if (random.Next(1)==0)
                {
                    await wowProcess.KeyPress(ConsoleKey.Spacebar, 498);
                }
                LastJump = DateTime.Now;
            }
        }


        DateTime lastFighting = DateTime.Now;

        public override void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.fighting)
            {
                lastFighting = DateTime.Now;
            }
        }
    }
}
