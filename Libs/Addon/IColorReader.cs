using System.Drawing;

namespace Libs
{
    public interface IColorReader
    {
        Color GetColorAt(Point point, Bitmap bmp);
    }
}