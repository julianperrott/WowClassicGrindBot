using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Libs
{
    public class WowProcess
    {
        private const UInt32 WM_KEYDOWN = 0x0100;
        private const UInt32 WM_KEYUP = 0x0101;
        private Random random = new Random();

        private Process _warcraftProcess;

        public Process WarcraftProcess
        {
            get
            {
                if ((DateTime.Now - LastIntialised).TotalSeconds > 10) // refresh every 10 seconds
                {
                    var process = Get();
                    if (process == null)
                    {
                        throw new ArgumentOutOfRangeException("Unable to find the Wow process");
                    }
                    this._warcraftProcess = process;
                }

                return this._warcraftProcess;
            }

            private set { _warcraftProcess = value; }
        }

        private DateTime LastIntialised = DateTime.Now.AddHours(-1);

        public WowProcess()
        {
            var process = Get();
            if (process == null)
            {
                throw new ArgumentOutOfRangeException("Unable to find the Wow process");
            }

            this._warcraftProcess = process;
            LastIntialised = DateTime.Now;
        }

        public bool IsWowClassic()
        {
            return WarcraftProcess.ProcessName.ToLower().Contains("classic");
        }

        //Get the wow-process, if success returns the process else null
        public static Process? Get(string name = "")
        {
            var names = string.IsNullOrEmpty(name) ? new List<string> { "Wow", "WowClassic", "Wow-64" } : new List<string> { name };

            var processList = Process.GetProcesses();
            foreach (var p in processList)
            {
                if (names.Contains(p.ProcessName))
                {
                    return p;
                }
            }

            //logger.Error($"Failed to find the wow process, tried: {string.Join(", ", names)}");

            return null;
        }

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        private void KeyDown(ConsoleKey key)
        {
            Debug.WriteLine($"KeyDown {key}");
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);
        }

        private void KeyUp(ConsoleKey key)
        {
            Debug.WriteLine($"KeyUp {key}");
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);
        }

        public async Task KeyPress(ConsoleKey key, int milliseconds)
        {
            Debug.WriteLine($"KeyPress {key} for {milliseconds}ms");
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);
            await Delay(milliseconds);
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);
        }

        public void SetKeyState(ConsoleKey key, bool pressDown)
        {
            if (pressDown) { KeyDown(key); } else { KeyUp(key); }
        }

        public void SetCursorPosition(System.Drawing.Point position)
        {
            SetCursorPos(position.X, position.Y);
        }

        public async Task RightClickMouse(System.Drawing.Point position)
        {
            SetCursorPosition(position);
            PostMessage(WarcraftProcess.MainWindowHandle, Keys.WM_RBUTTONDOWN, Keys.VK_RMB, 0);
            await Delay(101);
            PostMessage(WarcraftProcess.MainWindowHandle, Keys.WM_RBUTTONUP, Keys.VK_RMB, 0);
        }

        public async Task LeftClickMouse(System.Drawing.Point position)
        {
            SetCursorPosition(position);
            PostMessage(WarcraftProcess.MainWindowHandle, Keys.WM_LBUTTONDOWN, Keys.VK_RMB, 0);
            await Delay(101);
            PostMessage(WarcraftProcess.MainWindowHandle, Keys.WM_LBUTTONUP, Keys.VK_RMB, 0);
        }

        public async Task Delay(int milliseconds)
        {
            await Task.Delay(milliseconds + random.Next(1, 200));
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        public RECT GetWindowRect()
        {
            var handle = this.WarcraftProcess.MainWindowHandle;
            RECT rect = new RECT();
            GetWindowRect(handle, ref rect);
            return rect;
        }
    }
}