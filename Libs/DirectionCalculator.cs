using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Libs
{
    public class DirectionCalculator
    {
        public double CalculateHeading(WowPoint from, WowPoint to)
        {
            var target = Math.Atan2(to.X - from.X, to.Y - from.Y);
            return Math.PI+ target;
        }
    }
}
