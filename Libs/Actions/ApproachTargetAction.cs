using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class ApproachTargetAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;

        private DateTime LastJump = DateTime.Now;
        private Random random = new Random();
        
        public ApproachTargetAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;

            AddPrecondition(GoapKey.inmeleerange, false);
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, true);
        }

        public override float CostOfPerformingAction { get => 8f; }

        public override async Task PerformAction()
        {
            //await stopMoving.Stop();

            var location = playerReader.PlayerLocation;


            await this.wowProcess.KeyPress(ConsoleKey.H, 501);
            await RandomJump();
            await Task.Delay(500);

            var newLocation = playerReader.PlayerLocation;
            if (location.X == newLocation.X && location.Y == newLocation.Y)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, true);
                await Task.Delay(2000);
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
            }
            LastJump = DateTime.Now;
        }
    }
}
