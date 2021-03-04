using System.Drawing;
using System.Threading.Tasks;

namespace Libs
{
    public interface IRectProvider
    {
        void GetWindowRect(out Rectangle rect);
        Task RightClickMouse(Point position);
    }
}