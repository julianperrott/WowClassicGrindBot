using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UnitTests.SquareReaderTests
{
    public static class ColorHelper
    {
        public static Color LongToColour(long colorLong)
        {
            var value = colorLong;
            int red = (int)(value / 65536);
            value -= red * 65536;
            int green = (int)(value / 256);
            var blue = (int)value - (green * 256);
            return Color.FromArgb(red, green, blue);
        }
    }
}
