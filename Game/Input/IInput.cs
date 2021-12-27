using System.Drawing;
using System.Threading.Tasks;

namespace Game
{
    public interface IInput
    {
        void KeyDown(int key);

        void KeyUp(int key);

        ValueTask<int> KeyPress(int key, int milliseconds);

        ValueTask KeyPressNoDelay(int key, int milliseconds);

        void KeyPressSleep(int key, int milliseconds);

        void SetCursorPosition(Point p);

        ValueTask RightClickMouse(Point p);

        ValueTask LeftClickMouse(Point p);

        ValueTask SendText(string text);

        void PasteFromClipboard();
    }
}
