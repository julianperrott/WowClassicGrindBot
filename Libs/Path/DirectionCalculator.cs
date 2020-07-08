using System;

namespace Libs
{
    public static class DirectionCalculator
    {
        public static double CalculateHeading(WowPoint from, WowPoint to)
        {
            //logger.LogInformation($"from: ({from.X},{from.Y}) to: ({to.X},{to.Y})");

            var target = Math.Atan2(to.X - from.X, to.Y - from.Y);
            return Math.PI + target;
        }
    }
}