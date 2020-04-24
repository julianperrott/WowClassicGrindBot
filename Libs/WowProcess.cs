using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Libs
{
    public class WowProcess
    {
        private const UInt32 WM_KEYDOWN = 0x0100;
        private const UInt32 WM_KEYUP = 0x0101;
        private Random random = new Random();
        private ILogger logger;

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

        public WowProcess(ILogger logger)
        {
            this.logger = logger;

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

        private void KeyDown(ConsoleKey key, bool always)
        {
            if (keyDict.ContainsKey(key))
            {
                //if (keyDict[key] == true) { return; }
            }
            else
            {
                keyDict.Add(key, true);
            }

            logger.LogInformation($"KeyDown {key}");
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);

            keyDict[key] = true;
        }

        Dictionary<ConsoleKey, bool> keyDict = new Dictionary<ConsoleKey, bool>();


        private void KeyUp(ConsoleKey key, bool always)
        {
            if (keyDict.ContainsKey(key))
            {
                if (!always)
                {
                    if (keyDict[key] == false) { return; }
                }
            }
            else
            {
                keyDict.Add(key, false);
            }

            logger.LogInformation($"KeyUp {key}");
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);

            keyDict[key] = false;
        }

        public async void RightClickMouseBehindPlayer()
        {
            var rect = GetWindowRect();

            await RightClickMouse(new Point(rect.right / 2, (rect.bottom *2) / 3));
        }

        public async Task KeyPress(ConsoleKey key, int milliseconds, string description="")
        {
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            logger.LogInformation($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYDOWN, (int)key, 0);
            await Delay(milliseconds);
            PostMessage(WarcraftProcess.MainWindowHandle, WM_KEYUP, (int)key, 0);
        }

        public async Task TapStopKey()
        {
            await KeyPress(ConsoleKey.UpArrow, 51);
        }

        public async Task TapInteractKey()
        {
            logger.LogInformation($"Approach target");
            await KeyPress(ConsoleKey.H, 99);
        }

        public void SetKeyState(ConsoleKey key, bool pressDown, bool always=false)
        {
            if (pressDown) { KeyDown(key, always); } else { KeyUp(key, always); }
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

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public const int KEYEVENTF_KEYDOWN = 0x0000; // New definition
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_LCONTROL = 0xA2; //Left Control key code

        public const int VK_LEFT_SHIFT = 160;
        public const int VK_LEFT_CONTROL = 162;
        public const int VK_LEFT_ALT = 164;


        public const int A = 0x41; //A key code
        public const int C = 0x43; //C key code

        public async Task Hearthstone()
        {
            // hearth macro = /use hearthstone
            await KeyPress(ConsoleKey.I, 500);
        }

        public async Task Mount(PlayerReader playerReader)
        {
            // mount macro = /use 'your mount here'
            await KeyPress(ConsoleKey.O, 500);

            for (int i = 0; i < 40; i++)
            {
                if (playerReader.PlayerBitValues.IsMounted) { return; }
                if (playerReader.PlayerBitValues.PlayerInCombat) { return; }
                await Task.Delay(100);
            }
        }

        public async Task Dismount()
        {
            // mount macro = /use 'your mount here'
            await KeyPress(ConsoleKey.O, 500);
            await Task.Delay(1500);
        }
    }
}