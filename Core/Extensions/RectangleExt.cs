using System.Drawing;

namespace Core.Extensions
{
    public static class RectangleExt
    {
        public static Point Centre(this Rectangle r)
        {
            return new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
        }
    }

    public static class PointExt
    {
        public static Point Scale(this Point p, float scale)
        {
            return new Point((int)(p.X * scale), (int)(p.Y * scale));
        }

        public static Point Scale(this Point p, float scaleX, float scaleY)
        {
            return new Point((int)(p.X * scaleX), (int)(p.Y * scaleY));
        }
    }
}
