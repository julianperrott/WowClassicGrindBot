using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Libs
{
    public class WowPoint
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Vector2 Vector2 => new Vector2((float) X, (float) Y);

        public WowPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
