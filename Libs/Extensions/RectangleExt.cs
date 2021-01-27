using System.Drawing;

namespace Libs.Extensions
{
    public static class RectangleExt
    {
        public static Point Centre(this Rectangle r)
        {
            return new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
        }
    }
}
