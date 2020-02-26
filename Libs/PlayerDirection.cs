using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

        public void SetDirection(double desiredDirection)
        {
            var key = GetDirectionKeyToPress(desiredDirection);

            // Press Right
            wowProcess.SetKeyState(key, true);

            var startTime = DateTime.Now;

            // Wait until we are going the right direction
            while ((DateTime.Now-startTime).TotalSeconds<10)
            {
                var actualDirection = playerReader.Direction;

                bool closeEnoughToDesiredDirection = Math.Abs(actualDirection - desiredDirection) < 0.1;

                if (closeEnoughToDesiredDirection)
                {
                    Debug.WriteLine("Close enough, stopping turn");
                    wowProcess.SetKeyState(key, false);
                    break;
                }

                //bool goingTheWrongWay = GetDirectionKeyToPress(desiredDirection) != key;
                //if (goingTheWrongWay)
                //{
                //    Debug.WriteLine("GOING THE WRONG WAY!");
                //}
            }

            LastSetDirection = DateTime.Now;
        }

        private ConsoleKey GetDirectionKeyToPress(double desiredDirection)
        {
            var result = (RADIAN + desiredDirection - playerReader.Direction) % RADIAN < Math.PI
                ? ConsoleKey.LeftArrow : ConsoleKey.RightArrow;

            Debug.WriteLine($"Desired direction: {desiredDirection}, actual: {playerReader.Direction}, key: {result}");
            return result;
        }
    }
}
