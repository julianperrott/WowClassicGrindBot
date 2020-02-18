using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Libs
{
    public class DataFrame
    {
        public Point point { get; private set; }
        public int index { get; private set; }
        public DataFrame(Point point, int frame)
        {
            this.point = point;
            this.index = frame;
        }
    }
}
