using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Libs
{
    public class WowProcess
    {
        private const UInt32 WM_KEYDOWN = 0x0100;
        private const UInt32 WM_KEYUP = 0x0101;

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

        public void KeyDown(ConsoleKey key)
        {
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);
        }

        public void KeyUp(ConsoleKey key)
        {
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);
        }

        public async Task KeyPress(ConsoleKey key, int milliseconds)
        {
            this.KeyDown(key);
            await Task.Delay(milliseconds);
            this.KeyUp(key);
        }

        public void SetKeyState(ConsoleKey key, bool pressDown)
        {
            if (pressDown) { KeyDown(key); } else { KeyUp(key); }
        }

        public async Task RightClickMouse(System.Drawing.Point position)
        {
            System.Windows.Forms.Cursor.Position = position;
            PostMessage(WarcraftProcess.MainWindowHandle, Keys.WM_RBUTTONDOWN, Keys.VK_RMB, 0);
            await Task.Delay(101);
            PostMessage(WarcraftProcess.MainWindowHandle, Keys.WM_RBUTTONUP, Keys.VK_RMB, 0);
        }
    }
}
