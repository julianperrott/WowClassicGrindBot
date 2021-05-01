using System.Drawing;

namespace Core
{
    public interface IRectProvider
    {
        void GetPosition(out Point point);
        void GetRectangle(out Rectangle rect);
    }
}