using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs
{
    public class PlayerDirection : IPlayerDirection
    {
        private double RADIAN = Math.PI * 2;
        private WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        public DateTime LastSetDirection { get; private set; } = DateTime.Now.AddDays(-1);

        public PlayerDirection(PlayerReader playerReader, WowProcess wowProcess)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
        }

        public async Task SetDirection(double desiredDirection,WowPoint point, string source)
        {
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, point);

            Debug.WriteLine("===============");
            Debug.WriteLine($"SetDirection:- {source} Desired: {desiredDirection.ToString("0.000")}, Current: {playerReader.Direction.ToString("0.000")}, distance: {distance.ToString("0.000")}");

            if (distance < 40)
            {
                Debug.WriteLine("Too close, ignoring direction change.");
                return;
            }

            var key = GetDirectionKeyToPress(desiredDirection);

            // Press Right
            wowProcess.SetKeyState(key, true);

            var startTime = DateTime.Now;

            // Wait until we are going the right direction
            while ((DateTime.Now-startTime).TotalSeconds<10)
            {
                await Task.Delay(50);
                var actualDirection = playerReader.Direction;

                bool closeEnoughToDesiredDirection = Math.Abs(actualDirection - desiredDirection) < 0.01;

                if (closeEnoughToDesiredDirection)
                {
                    Debug.WriteLine("Close enough, stopping turn");
                    wowProcess.SetKeyState(key, false);
                    break;
                }

                bool goingTheWrongWay = GetDirectionKeyToPress(desiredDirection) != key;
                if (goingTheWrongWay)
                {
                    Debug.WriteLine("GOING THE WRONG WAY!");
                    wowProcess.SetKeyState(key, false);
                    break;
                }
            }

            LastSetDirection = DateTime.Now;
        }

        string lastText = string.Empty;

        private ConsoleKey GetDirectionKeyToPress(double desiredDirection)
        {
            var result = (RADIAN + desiredDirection - playerReader.Direction) % RADIAN < Math.PI
                ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow;

            var text = $"GetDirectionKeyToPress: Desired direction: {desiredDirection}, actual: {playerReader.Direction}, key: {result}";

            if (text != lastText)
            {
                Debug.WriteLine(text);
            }

            lastText = text;
            return result;
        }
    }
}
