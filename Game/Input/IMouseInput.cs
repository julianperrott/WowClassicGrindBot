using System.Drawing;
using System.Threading.Tasks;

namespace Game
{
    public interface IMouseInput
    {
        void SetCursorPosition(Point point);

        ValueTask RightClickMouse(Point position);

        ValueTask LeftClickMouse(Point position);
    }
}
