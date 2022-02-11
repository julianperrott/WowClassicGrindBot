using System.Drawing;

namespace Game
{
    public interface IInput
    {
        void KeyDown(int key);

        void KeyUp(int key);

        int KeyPress(int key, int milliseconds);

        void KeyPressSleep(int key, int milliseconds);

        void SetCursorPosition(Point p);

        void RightClickMouse(Point p);

        void LeftClickMouse(Point p);

        void SendText(string text);

        void PasteFromClipboard();
    }
}
