using System.Drawing;

namespace SharedLib
{
    public interface IRectProvider
    {
        void GetPosition(out Point point);
        void GetRectangle(out Rectangle rect);
    }
}