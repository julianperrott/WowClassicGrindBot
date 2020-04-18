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
            PlayerClassEnum.Priest => this.playerReader.ActionBarUseable_1To24.HotKey8 ? ConsoleKey.D8 : ConsoleKey.Escape,
            PlayerClassEnum.Druid => this.playerReader.ActionBarUseable_1To24.HotKey8 ? ConsoleKey.D8 : ConsoleKey.Escape,
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
                if (playerReader.PlayerClass == PlayerClassEnum.Druid)
                {
                    if (this.playerReader.Druid_ShapeshiftForm != ShapeshiftForm.None)
                    {
                        await this.wowProcess.KeyPress(ConsoleKey.F8, 100); // cancelform
                    }
                }

                await wowProcess.KeyPress(key, 500);
            }

            await Task.Delay(1000);

            bool hasDrank = false;

            for (int i = 0; i < seconds; i++)
            {
                hasDrank = hasDrank || this.playerReader.Buffs.Drinking;

                if (this.playerReader.ManaPercentage > 98 || !this.playerReader.Buffs.Drinking)
                {
                    if (hasDrank)
                    {
                        await wowProcess.TapStopKey();
                    }

                    if (playerReader.PlayerClass == PlayerClassEnum.Druid)
                    {
                        RaiseEvent(new ActionEvent(GoapKey.postloot, true));
                    }
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
