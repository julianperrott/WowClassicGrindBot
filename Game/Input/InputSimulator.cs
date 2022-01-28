using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using WinAPI;

namespace Game
{
    public class InputSimulator : IInput
    {
        private readonly int MIN_DELAY;
        private readonly int MAX_DELAY;

        private readonly Random random = new();
        private readonly GregsStack.InputSimulatorStandard.InputSimulator simulator;
        private readonly Process process;

        public InputSimulator(Process process, int minDelay, int maxDelay)
        {
            this.process = process;

            MIN_DELAY = minDelay;
            MAX_DELAY = maxDelay;

            simulator = new GregsStack.InputSimulatorStandard.InputSimulator();
        }

        private int Delay(int milliseconds)
        {
            int delay = milliseconds + random.Next(1, MAX_DELAY);
            Thread.Sleep(delay);
            return delay;
        }

        public void KeyDown(int key)
        {
            if(NativeMethods.GetForegroundWindow() != process.MainWindowHandle)
                NativeMethods.SetForegroundWindow(process.MainWindowHandle);

            simulator.Keyboard.KeyDown((VirtualKeyCode)key);
        }

        public void KeyUp(int key)
        {
            if (NativeMethods.GetForegroundWindow() != process.MainWindowHandle)
                NativeMethods.SetForegroundWindow(process.MainWindowHandle);

            simulator.Keyboard.KeyUp((VirtualKeyCode)key);
        }

        public int KeyPress(int key, int milliseconds)
        {
            simulator.Keyboard.KeyDown((VirtualKeyCode)key);
            int delay = Delay(milliseconds);
            simulator.Keyboard.KeyUp((VirtualKeyCode)key);
            return delay;
        }

        public void KeyPressSleep(int key, int milliseconds)
        {
            simulator.Keyboard.KeyDown((VirtualKeyCode)key);
            Thread.Sleep(milliseconds);
            simulator.Keyboard.KeyUp((VirtualKeyCode)key);
        }

        public void LeftClickMouse(Point p)
        {
            SetCursorPosition(p);
            simulator.Mouse.LeftButtonDown();
            Delay(MIN_DELAY);
            simulator.Mouse.LeftButtonUp();
        }

        public void RightClickMouse(Point p)
        {
            SetCursorPosition(p);
            simulator.Mouse.RightButtonDown();
            Delay(MIN_DELAY);
            simulator.Mouse.RightButtonUp();
        }

        public void SetCursorPosition(Point p)
        {
            NativeMethods.GetWindowRect(process.MainWindowHandle, out var rect);
            p.X = p.X * 65535 / rect.Width;
            p.Y = p.Y * 65535 / rect.Height;
            simulator.Mouse.MoveMouseTo(Convert.ToDouble(p.X), Convert.ToDouble(p.Y));
        }

        public void SendText(string text)
        {
            if (NativeMethods.GetForegroundWindow() != process.MainWindowHandle)
                NativeMethods.SetForegroundWindow(process.MainWindowHandle);

            simulator.Keyboard.TextEntry(text);
            Delay(25);
        }

        public void PasteFromClipboard()
        {
            simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_V);
        }
    }
}
