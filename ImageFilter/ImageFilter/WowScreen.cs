using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ImageFilter
{
    public static class WowScreen
    {

        public static Bitmap GetBitmap(int width = 1920, int height = 1080)
        {
            var bmpScreen = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, bmpScreen.Size);
            }
            return bmpScreen;
        }


    }
}
