using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinAPI;

namespace Core
{
    public class InputWindowsNative : IInput
    {
        private const int MAX_DELAY = 200;
        private const int MAX_MOUSE_DELAY = 75;

        private readonly Process process;
        private readonly Random random = new Random();

        private readonly IEnumerable<ConsoleKey> consoleKeys = (IEnumerable<ConsoleKey>)Enum.GetValues(typeof(ConsoleKey));

        public InputWindowsNative(Process process)
        {
            this.process = process;
        }

        private async Task Delay(int milliseconds)
        {
            await Task.Delay(milliseconds + random.Next(1, MAX_DELAY));
        }

        public void KeyDown(int key)
        {
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
        }

        public void KeyUp(int key)
        {
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public async Task KeyPress(int key, int milliseconds)
        {
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
            await Delay(milliseconds);
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public void KeyPressSleep(int key, int milliseconds)
        {
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
            Thread.Sleep(milliseconds);
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public async Task LeftClickMouse(Point position)
        {
            SetCursorPosition(position);
            await Delay(MAX_MOUSE_DELAY);
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_LBUTTONDOWN, NativeMethods.VK_RMB, 0);
            await Delay(MAX_MOUSE_DELAY);
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_LBUTTONUP, NativeMethods.VK_RMB, 0);
            await Delay(MAX_MOUSE_DELAY);
        }

        public async Task RightClickMouse(Point position)
        {
            SetCursorPosition(position);
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_RBUTTONDOWN, NativeMethods.VK_RMB, 0);
            await Delay(MAX_MOUSE_DELAY);
            NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_RBUTTONUP, NativeMethods.VK_RMB, 0);
        }

        public void SetCursorPosition(Point position)
        {
            NativeMethods.SetCursorPos(position.X, position.Y);
        }

        public async Task SendText(string text)
        {
            // only works with ConsoleKey characters
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var consoleKey = consoleKeys.FirstOrDefault(k => k.ToString() == chars[i].ToString());
                if(consoleKey != 0)
                { 
                    await KeyPress((int)consoleKey, 15);
                }
            }
        }

        public void PasteFromClipboard()
        {
            // currently not supported
            throw new NotImplementedException();
        }
    }
}
