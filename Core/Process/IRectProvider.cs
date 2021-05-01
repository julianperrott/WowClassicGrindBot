using System.Drawing;

namespace Core
{
    public interface IRectProvider
    {
        void GetWindowRect(out Rectangle rect);
    }
}