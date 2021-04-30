using System.Drawing;
using System.Threading.Tasks;

namespace Libs
{
    public interface IMouseInput
    {
        void SetCursorPosition(Point point);

        Task RightClickMouse(Point position);

        Task LeftClickMouse(Point position);
    }
}
