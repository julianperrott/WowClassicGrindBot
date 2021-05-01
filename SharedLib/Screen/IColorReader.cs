using System.Drawing;

namespace SharedLib
{
    public interface IColorReader
    {
        Color GetColorAt(Point point);

        Bitmap GetBitmap(int width, int height);
    }
}