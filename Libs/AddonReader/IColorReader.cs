using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Libs
{
    public interface IColorReader
    {
        Color GetColorAt(Point point, Bitmap bmp);
    }
}
