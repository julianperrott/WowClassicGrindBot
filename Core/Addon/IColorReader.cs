using System.Drawing;

namespace Core
{
    public interface IColorReader
    {
        Color GetColorAt(Point point, Bitmap bmp);

        Bitmap GetBitmap(int width, int height);
    }
}