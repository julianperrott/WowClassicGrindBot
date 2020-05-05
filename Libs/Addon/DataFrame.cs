using System.Drawing;

namespace Libs
{
    public class DataFrame
    {
        public Point point { get; private set; }
        public int index { get; private set; }

        public DataFrame(Point point, int index)
        {
            this.point = point;
            this.index = index;
        }
    }
}