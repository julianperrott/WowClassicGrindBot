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
    public class WowProcess : IRectProvider
    {
        private Random random = new Random();
        private ILogger logger;

        private bool debug = true;

        private Process _warcraftProcess;

        public Process WarcraftProcess
        {
            get
            {
                if (this._warcraftProcess == null)
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
        }

        public WowProcess(ILogger logger)
        {
            this.logger = logger;

            var process = Get();
            if (process == null)
            {
                throw new ArgumentOutOfRangeException("Unable to find the Wow process");
            }

            this._warcraftProcess = process;
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

        private void KeyDown(ConsoleKey key, string description = "")
        {
            if (keyDict.ContainsKey(key))
            {
                //if (keyDict[key] == true) { return; }
            }
            else
            {
                keyDict.Add(key, true);
            }

            if(!string.IsNullOrEmpty(description))
                Log($"KeyDown {key} "+ description);

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

            Log($"KeyUp {key}");
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);

            keyDict[key] = false;
        }

        public async void RightClickMouseBehindPlayer()
        {
            GetWindowRect(out var rect);

            await RightClickMouse(new Point(rect.Right / 2, (rect.Bottom * 2) / 3));
        }

        public void SetForegroundWindow()
        {
            NativeMethods.SetForegroundWindow(WarcraftProcess.MainWindowHandle);
        }

        public void SendKeys(string payload)
        {
            try {
                Process p = new Process();
                p.StartInfo.FileName = "SendKeys.exe";
                string args = $"-pid:{_warcraftProcess.Id} \"{payload}\"";
                p.StartInfo.Arguments = args;

                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();

                p.WaitForExit();
                p.Dispose();
            } 
            catch(Exception e)
            {
                logger.LogDebug(e.Message);
            }
        }

        public async Task KeyPress(ConsoleKey key, int milliseconds, string description = "")
        {
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            Log($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
            await Delay(milliseconds);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public void KeyPressSleep(ConsoleKey key, int milliseconds, string description = "")
        {
            if (milliseconds < 1) { return; }
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            Log($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYDOWN, (int)key, 0);
            Thread.Sleep(milliseconds);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_KEYUP, (int)key, 0);
        }

        public void SetKeyState(ConsoleKey key, bool pressDown, bool forceClick, string description = "")
        {
            if(!string.IsNullOrEmpty(description))
                Log($"SetKeyState: {description}");

            if (pressDown) { KeyDown(key, description); } else { KeyUp(key, forceClick); }
        }

        public static void SetCursorPosition(System.Drawing.Point position)
        {
            NativeMethods.SetCursorPos(position.X, position.Y);
        }

        public async Task RightClickMouse(System.Drawing.Point position)
        {
            SetCursorPosition(position);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_RBUTTONDOWN, NativeMethods.VK_RMB, 0);
            await Delay(75);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_RBUTTONUP, NativeMethods.VK_RMB, 0);
        }

        public async Task LeftClickMouse(System.Drawing.Point position)
        {
            SetCursorPosition(position);
            await Delay(75);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_LBUTTONDOWN, NativeMethods.VK_RMB, 0);
            await Delay(75);
            NativeMethods.PostMessage(WarcraftProcess.MainWindowHandle, NativeMethods.WM_LBUTTONUP, NativeMethods.VK_RMB, 0);
            await Delay(75);
        }

        public async Task Delay(int milliseconds)
        {
            await Task.Delay(milliseconds + random.Next(1, 200));
        }

        public void GetWindowRect(out Rectangle rect)
        {
            var handle = this.WarcraftProcess.MainWindowHandle;
            NativeMethods.GetWindowRect(handle, out rect);
        }

        int scale = 100;

        public int ScaleUp(int value)
        {
            if (scale == 100)
            {
                return value;
            }

            return (value * scale) / 100;
        }

        public int ScaleDown(int value)
        {
            if (scale == 100)
            {
                return value;
            }

            return (value * 100) / scale;
        }

        private void Log(string text)
        {
            if (debug)
                logger.LogInformation($"{this.GetType().Name}: {text}");
        }
    }
}