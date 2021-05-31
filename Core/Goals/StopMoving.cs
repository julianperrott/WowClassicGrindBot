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

        private const double MinDist = 0.1;

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
                if(!input.IsKeyDown(ConsoleKey.DownArrow) && !input.IsKeyDown(ConsoleKey.UpArrow) &&
                    (Math.Abs(XCoord - playerReader.XCoord) > MinDist || Math.Abs(XCoord - playerReader.XCoord) > MinDist))
                {
                    input.SetKeyState(ConsoleKey.DownArrow, true, false, $"StopMoving - Cancel interact dx:{Math.Abs(XCoord - playerReader.XCoord),6} dy:{Math.Abs(XCoord - playerReader.XCoord),6}");
                    await Task.Delay(1);
                }

                input.SetKeyState(ConsoleKey.UpArrow, false, false, "");
                input.SetKeyState(ConsoleKey.DownArrow, false, false, "StopMoving");
                await Task.Delay(1);
            }

            this.XCoord = playerReader.XCoord;
            this.YCoord = playerReader.YCoord;

            await StopTurn();
        }

        public async Task StopTurn()
        {
            if (Direction != playerReader.Direction)
            {
                input.SetKeyState(ConsoleKey.LeftArrow, false, false, "");
                await Task.Delay(1);
                input.SetKeyState(ConsoleKey.RightArrow, false, false, "StopTurn");
                await Task.Delay(1);
            }

            this.Direction = playerReader.Direction;
        }
    }
}