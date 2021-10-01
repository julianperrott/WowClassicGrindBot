using System.Drawing;

namespace SharedLib.Extensions
{
    public static class RectangleExt
    {
        public static Point Centre(this Rectangle r)
        {
            return new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
        }

        public static Point BottomCentre(this Rectangle r)
        {
            return new Point(r.Left + r.Width / 2, r.Bottom);
        }

        public static double SqrDistance(Point p1, Point p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
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
