using System;
using System.Numerics;

namespace Core
{
    public static class DirectionCalculator
    {
        public static float CalculateHeading(Vector3 from, Vector3 to)
        {
            //logger.LogInformation($"from: ({from.X},{from.Y}) to: ({to.X},{to.Y})");

            var target = MathF.Atan2(to.X - from.X, to.Y - from.Y);
            return MathF.PI + target;
        }

        public static (float, float) ToNormalRadian(float wowRadian)
        {
            // wow origo is north side - shifted 90 degree
            return (
                MathF.Cos(wowRadian + (MathF.PI / 2)),
                MathF.Sin(wowRadian - (MathF.PI / 2)));
        }
    }
}