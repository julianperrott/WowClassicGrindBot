using Microsoft.Extensions.Logging;
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
        private ILogger logger;

        public PlayerDirection(PlayerReader playerReader, WowProcess wowProcess, ILogger logger)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.logger = logger;
        }

        public async Task SetDirection(double desiredDirection,WowPoint point, string source)
        {
            var location = new WowPoint(playerReader.XCoord, playerReader.YCoord);
            var distance = WowPoint.DistanceTo(location, point);

            logger.LogInformation("===============");
            logger.LogInformation($"SetDirection:- {source} Desired: {desiredDirection.ToString("0.000")}, Current: {playerReader.Direction.ToString("0.000")}, distance: {distance.ToString("0.000")}");

            if (distance < 40)
            {
                logger.LogInformation("Too close, ignoring direction change.");
                return;
            }

            var key = GetDirectionKeyToPress(desiredDirection);

            // Press Right
            wowProcess.SetKeyState(key, true, true, "PlayerDirection");

            var startTime = DateTime.Now;

            // Wait until we are going the right direction
            while ((DateTime.Now-startTime).TotalSeconds<10)
            {
                if((DateTime.Now - startTime).TotalSeconds>10)
                {
                    await Task.Delay(1);
                }
                System.Threading.Thread.Sleep(1);
                var actualDirection = playerReader.Direction;

                bool closeEnoughToDesiredDirection = Math.Abs(actualDirection - desiredDirection) < 0.01;

                if (closeEnoughToDesiredDirection)
                {
                    logger.LogInformation("Close enough, stopping turn");
                    wowProcess.SetKeyState(key, false, true, "PlayerDirection");
                    break;
                }

                bool goingTheWrongWay = GetDirectionKeyToPress(desiredDirection) != key;
                if (goingTheWrongWay)
                {
                    logger.LogInformation("GOING THE WRONG WAY! Stop turn");
                    wowProcess.SetKeyState(key, false, true, "PlayerDirection");
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
                //logger.LogInformation(text);
            }

            lastText = text;
            return result;
        }
    }
}
