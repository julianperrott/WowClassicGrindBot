using System.Drawing;
using System.Threading.Tasks;

namespace Core
{
    public interface IMouseInput
    {
        void SetCursorPosition(Point point);

        Task RightClickMouse(Point position);

        Task LeftClickMouse(Point position);
    }
}
