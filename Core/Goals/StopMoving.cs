using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Game;

namespace Core.Goals
{
    public class StopMoving
    {
        private readonly WowProcessInput input;
        private readonly PlayerReader playerReader;

        private double XCoord = 0;
        private double YCoord = 0;
        private double Direction = 0;

        public StopMoving(WowProcessInput input, PlayerReader playerReader)
        {
            this.input = input;
            this.playerReader = playerReader;
        }

        public async Task Stop()
        {
            if (XCoord != playerReader.XCoord || YCoord != playerReader.YCoord)
            {
                input.SetKeyState(ConsoleKey.UpArrow, false, false, "StopMoving");
                await Task.Delay(1);
            }

            if (Direction != playerReader.Direction)
            {
                input.SetKeyState(ConsoleKey.LeftArrow, false, false, "StopMoving");
                await Task.Delay(1);
                input.SetKeyState(ConsoleKey.RightArrow, false, false, "StopMoving");
                await Task.Delay(1);
            }

            this.Direction = playerReader.Direction;
            this.XCoord = playerReader.XCoord;
            this.YCoord = playerReader.YCoord;
        }
    }
}