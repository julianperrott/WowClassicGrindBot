using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Game
{
    public interface IInput
    {
        void KeyDown(int key);

        void KeyUp(int key);

        Task KeyPress(int key, int milliseconds);

        void KeyPressSleep(int key, int milliseconds);

        void SetCursorPosition(Point p);

        Task RightClickMouse(Point p);

        Task LeftClickMouse(Point p);

        Task SendText(string text);

        void PasteFromClipboard();
    }
}
