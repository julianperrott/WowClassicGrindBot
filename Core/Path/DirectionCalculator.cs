using System;

namespace Core
{
    public static class DirectionCalculator
    {
        public static double CalculateHeading(WowPoint from, WowPoint to)
        {
            //logger.LogInformation($"from: ({from.X},{from.Y}) to: ({to.X},{to.Y})");

            var target = Math.Atan2(to.X - from.X, to.Y - from.Y);
            return Math.PI + target;
        }

        public static Tuple<double, double> ToNormalRadian(double wowRadian)
        {
            // wow origo is north side - shifted 90 degree
            return new Tuple<double, double>(
                Math.Cos(wowRadian + (Math.PI / 2)),
                Math.Sin(wowRadian - (Math.PI / 2)));
        }
    }
}