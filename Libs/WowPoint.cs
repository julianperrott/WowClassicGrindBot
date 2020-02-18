using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class WowPoint
    {
        public double X { get; private set; }
        public double Y { get; private set; }

        public WowPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
