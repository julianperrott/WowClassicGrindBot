using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class StopMoving
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;

        private double XCoord = 0;
        private double YCoord = 0;
        private double Direction = 0;

        public StopMoving(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
        }

        public async Task Stop()
        {
            if (XCoord != playerReader.XCoord || YCoord != playerReader.YCoord)
            {
                wowProcess.SetKeyState(ConsoleKey.UpArrow, false, false, "StopMoving");
                await Task.Delay(1);
            }

            if (Direction != playerReader.Direction)
            {
                wowProcess.SetKeyState(ConsoleKey.LeftArrow, false, false, "StopMoving");
                await Task.Delay(1);
                wowProcess.SetKeyState(ConsoleKey.RightArrow, false, false, "StopMoving");
                await Task.Delay(1);
            }

            this.Direction = playerReader.Direction;
            this.XCoord = playerReader.XCoord;
            this.YCoord = playerReader.YCoord;
        }
    }
}