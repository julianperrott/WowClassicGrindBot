using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Game;

namespace Core
{
    public class PlayerDirection : IPlayerDirection
    {
        private readonly ILogger logger;
        private readonly WowProcessInput input;
        private readonly PlayerReader playerReader;

        public DateTime LastSetDirection { get; private set; } = DateTime.Now.AddDays(-1);
        private double RADIAN = Math.PI * 2;

        private const int DefaultIgnoreDistance = 15;


        private bool debug = false;

        public PlayerDirection(ILogger logger, WowProcessInput input, PlayerReader playerReader)
        {
            this.logger = logger;
            this.input = input;
            this.playerReader = playerReader;
        }

        public async Task SetDirection(double desiredDirection, WowPoint point, string source)
        {
            await SetDirection(desiredDirection, point, source, DefaultIgnoreDistance);
        }

        public async Task SetDirection(double desiredDirection, WowPoint point, string source, int ignoreDistance)
        {
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, point);

            if(!string.IsNullOrEmpty(source))
                Log($"SetDirection:- {source} Desired: {desiredDirection.ToString("0.000")}, Current: {playerReader.Direction.ToString("0.000")}, distance: {distance.ToString("0.000")}");

            if (distance < ignoreDistance)
            {
                Log("Too close, ignoring direction change.");
                return;
            }

            var key = GetDirectionKeyToPress(desiredDirection);

            TurnUsingTimedPress(desiredDirection, key);

            //await TurnAndReadActualDirection(desiredDirection, key);
            await Task.Delay(1);

            LastSetDirection = DateTime.Now;
        }

        private void TurnUsingTimedPress(double desiredDirection, ConsoleKey key)
        {
            input.KeyPressSleep(key, TurnDuration(desiredDirection), "TurnUsingTimedPress");
        }

        public double TurnAmount(double desiredDirection)
        {
            double RADIAN = Math.PI * 2;
            var result = (RADIAN + desiredDirection - playerReader.Direction) % RADIAN;
            if (result > Math.PI) { result = RADIAN - result; }
            return result;
        }

        public int TurnDuration(double desiredDirection)
        {
            return (int)((TurnAmount(desiredDirection) * 1000) / Math.PI);
        }

        private async Task TurnAndReadActualDirection(double desiredDirection, ConsoleKey key)
        {
            // Press Right
            input.SetKeyState(key, true, true, "PlayerDirection");

            var startTime = DateTime.Now;

            // Wait until we are going the right direction
            while ((DateTime.Now - startTime).TotalSeconds < 10)
            {
                if ((DateTime.Now - startTime).TotalSeconds > 10)
                {
                    await Task.Delay(1);
                }
                System.Threading.Thread.Sleep(1);
                var actualDirection = playerReader.Direction;

                bool closeEnoughToDesiredDirection = Math.Abs(actualDirection - desiredDirection) < 0.01;

                if (closeEnoughToDesiredDirection)
                {
                    Log("Close enough, stopping turn");
                    input.SetKeyState(key, false, true, "PlayerDirection");
                    break;
                }

                bool goingTheWrongWay = GetDirectionKeyToPress(desiredDirection) != key;
                if (goingTheWrongWay)
                {
                    Log("GOING THE WRONG WAY! Stop turn");
                    input.SetKeyState(key, false, true, "PlayerDirection");
                    break;
                }
            }
        }

        private string lastText = string.Empty;

        private ConsoleKey GetDirectionKeyToPress(double desiredDirection)
        {
            var result = (RADIAN + desiredDirection - playerReader.Direction) % RADIAN < Math.PI
                ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow;

            var text = $"GetDirectionKeyToPress: Desired direction: {desiredDirection}, actual: {playerReader.Direction}, key: {result}";

            if (text != lastText)
            {
                //Log(text);
            }

            lastText = text;
            return result;
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }
    }
}