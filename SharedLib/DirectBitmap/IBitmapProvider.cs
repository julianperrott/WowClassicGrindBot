using System.Drawing;

namespace SharedLib
{
    public interface IBitmapProvider
    {
        Bitmap Bitmap { get; }

        Rectangle Rect { get; }
    }
}
