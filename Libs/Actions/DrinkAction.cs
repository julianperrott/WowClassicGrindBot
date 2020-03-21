using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class DrinkAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private ILogger logger;

        public DrinkAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.logger = logger;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.shoulddrink, true);
        }

        public override float CostOfPerformingAction { get => 4f; }

        private ConsoleKey Drink => playerReader.PlayerClass switch
        {
            PlayerClassEnum.Priest => this.playerReader.ActionBarUseable_1To24.HotKey7 ? ConsoleKey.D8 : ConsoleKey.Escape,
            _ => ConsoleKey.Escape
        };

        public override async Task PerformAction()
        {
            await stopMoving.Stop();
            await PressKeyAndWait(Drink, 30);
        }

        private async Task PressKeyAndWait(ConsoleKey key, int seconds)
        {
            if (key != ConsoleKey.Escape)
            {
                await wowProcess.KeyPress(key, 500);
            }

            await Task.Delay(1000);

            for (int i = 0; i < seconds; i++)
            {
                if (this.playerReader.ManaPercentage > 98 || !this.playerReader.Buffs.Drinking)
                {
                    await wowProcess.KeyPress(ConsoleKey.UpArrow, 400);
                    break;
                }
                if (this.playerReader.PlayerBitValues.PlayerInCombat)
                {
                    return;
                }

                await Task.Delay(1000);
            }
        }
    }
}
