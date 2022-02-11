using System.Drawing;

namespace Game
{
    public interface IMouseInput
    {
        void SetCursorPosition(Point point);

        void RightClickMouse(Point position);

        void LeftClickMouse(Point position);
    }
}
