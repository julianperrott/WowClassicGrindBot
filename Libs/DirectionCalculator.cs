using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace Libs
{
    public class DirectionCalculator
    {
        public double CalculateHeading(WowPoint from, WowPoint to)
        {
            Debug.WriteLine($"from: ({from.X},{from.Y}) to: ({to.X},{to.Y})");

            var target = Math.Atan2(to.X - from.X, to.Y - from.Y);
            return Math.PI+ target;
        }
    }
}
