using System.Drawing;

namespace Libs
{
    public class NpcPosition
    {
        public Point Min { get; set; }
        public Point Max { get; set; }
        public int Height => Max.Y - Min.Y;
        public int Width => Max.X - Min.X;
        private int screenMid;
        private int screenMidBuffer;
        public bool IsAdd => ClickPoint.X < screenMid- screenMidBuffer || ClickPoint.X > screenMid + screenMidBuffer;

        public Point ClickPoint => new Point(Min.X + (Width / 2), Max.Y + 45);

        public NpcPosition(Point min, Point max, int screenWidth)
        {
            this.Min = min;
            this.Max = max;
            this.screenMid = screenWidth/2;
            this.screenMidBuffer = screenWidth / 10;
        }
    }
}