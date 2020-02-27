using System;
using System.Drawing;

namespace Libs
{
    public class WowScreen : IColorReader
    {
        public static Bitmap GetAddonBitmap(int width = 300, int height = 200)
        {
            var bmpScreen = new Bitmap(width, height);
            var graphics = Graphics.FromImage(bmpScreen);
            graphics.CopyFromScreen(0, 0, 0, 0, bmpScreen.Size);
            graphics.Dispose();
            return bmpScreen;
        }

        public Color GetColorAt(Point pos, Bitmap bmp)
        {
            return bmp.GetPixel(pos.X, pos.Y);
        }
    }
}
