using System;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class StopMoving
    {
        private readonly ConfigurableInput input;
        private readonly PlayerReader playerReader;

        private const double MinDist = 0.01;

        private float XCoord;
        private float YCoord;
        private float Direction;

        public StopMoving(ConfigurableInput input, PlayerReader playerReader)
        {
            this.input = input;
            this.playerReader = playerReader;
        }

        public async ValueTask Stop()
        {
            await StopForward();
            await StopTurn();
        }

        public async ValueTask StopForward()
        {
            if (XCoord != playerReader.XCoord || YCoord != playerReader.YCoord)
            {
                if (!input.IsKeyDown(input.BackwardKey) && !input.IsKeyDown(input.ForwardKey) &&
                    (MathF.Abs(XCoord - playerReader.XCoord) > MinDist || MathF.Abs(YCoord - playerReader.YCoord) > MinDist))
                {
                    input.SetKeyState(input.ForwardKey, true, false, "StopForward - Cancel interact");
                    await Task.Delay(1);
                }

                input.SetKeyState(input.ForwardKey, false, false, "");
                input.SetKeyState(input.BackwardKey, false, false, "StopForward");
                await Task.Delay(10);
            }

            this.XCoord = playerReader.XCoord;
            this.YCoord = playerReader.YCoord;
        }

        public async ValueTask StopTurn()
        {
            if (Direction != playerReader.Direction)
            {
                input.SetKeyState(input.TurnLeftKey, false, false, "");
                await Task.Delay(1);
                input.SetKeyState(input.TurnRightKey, false, false, "StopTurn");
                await Task.Delay(1);
            }

            this.Direction = playerReader.Direction;
        }
    }
}