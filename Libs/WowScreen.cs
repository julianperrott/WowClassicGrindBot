using System;
using System.Drawing;

namespace Libs
{
    public class WowScreen : IColorReader
    {
        public static Bitmap GetAddonBitmap(int width = 500, int height = 200)
        {
            var bmpScreen = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, bmpScreen.Size);
            }
            return bmpScreen;
        }

        public Color GetColorAt(Point pos, Bitmap bmp)
        {
            var color= bmp.GetPixel(pos.X, pos.Y);

            return color;
        }
    }
}
