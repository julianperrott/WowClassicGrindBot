using Libs.GOAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class BuffAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;

        private DateTime LastSharpened = DateTime.Now.AddDays(-1);

        public BuffAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;

            AddPrecondition(GoapKey.incombat, false);
        }

        public override float CostOfPerformingAction { get => 1f; }

        private ConsoleKey SharpenWeapon => this.playerReader.ActionBarUseable_73To96.HotKey12 ? ConsoleKey.OemPlus : ConsoleKey.Escape;

        public override async Task PerformAction()
        {
            if (SharpenWeapon != ConsoleKey.Escape)
            {
                await this.stopMoving.Stop();

                Debug.WriteLine("Sharening weapon");
                await wowProcess.KeyPress(SharpenWeapon, 500);

                for (int i = 0; i < 7; i++)
                {
                    await Task.Delay(1000);
                    if (playerReader.PlayerBitValues.PlayerInCombat) { return; }
                }
            }

            LastSharpened = DateTime.Now;
        }

        public override bool CheckIfActionCanRun()
        {
            return (DateTime.Now - LastSharpened).TotalMinutes > 31;
        }
    }
}
