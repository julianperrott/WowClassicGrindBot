using System.Drawing;

namespace Libs
{
    public interface IRectProvider
    {
        void GetWindowRect(out Rectangle rect);
    }
}