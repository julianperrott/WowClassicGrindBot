using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Libs
{
    public class WowProcess
    {
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

        private void KeyDown(ConsoleKey key)
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
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);

            keyDict[key] = true;
        }

        private Dictionary<ConsoleKey, bool> keyDict = new Dictionary<ConsoleKey, bool>();

        private void KeyUp(ConsoleKey key, bool forceClick)
        {
            if (keyDict.ContainsKey(key))
            {
                if (!forceClick)
                {
                    if (keyDict[key] == false) { return; }
                }
            }
            else
            {
                keyDict.Add(key, false);
            }

            logger.LogInformation($"KeyUp {key}");
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);

            keyDict[key] = false;
        }

        public async void RightClickMouseBehindPlayer()
        {
            var rect = GetWindowRect();

            await RightClickMouse(new Point(rect.right / 2, (rect.bottom * 2) / 3));
        }

        public async Task KeyPress(ConsoleKey key, int milliseconds, string description = "")
        {
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            logger.LogInformation($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
            await Delay(milliseconds);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public void KeyPressSleep(ConsoleKey key, int milliseconds, string description = "")
        {
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            logger.LogInformation($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
            Thread.Sleep(milliseconds);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public async Task TapStopKey()
        {
            await KeyPress(ConsoleKey.UpArrow, 51);
        }

        public void SetKeyState(ConsoleKey key, bool pressDown, bool forceClick, string description)
        {
            Debug.WriteLine("SetKeyState: " + description);
            if (pressDown) { KeyDown(key); } else { KeyUp(key, forceClick); }
        }

        public static void SetCursorPosition(System.Drawing.Point position)
        {
            NativeMethods.SetCursorPos(position.X, position.Y);
        }

        public async Task RightClickMouse(System.Drawing.Point position)
        {
            SetCursorPosition(position);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_RBUTTONDOWN, NativeMethods.VK_RMB, 0);
            await Delay(101);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_RBUTTONUP, NativeMethods.VK_RMB, 0);
        }

        public async Task LeftClickMouse(System.Drawing.Point position)
        {
            SetCursorPosition(position);
            await Delay(101);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_LBUTTONDOWN, NativeMethods.VK_RMB, 0);
            await Delay(101);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_LBUTTONUP, NativeMethods.VK_RMB, 0);
            await Delay(101);
        }

        public async Task Delay(int milliseconds)
        {
            await Task.Delay(milliseconds + random.Next(1, 200));
        }

        public RECT GetWindowRect()
        {
            var handle = this.WarcraftProcess.MainWindowHandle;
            RECT rect = new RECT();
            NativeMethods.GetWindowRect(handle, ref rect);
            return rect;
        }

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