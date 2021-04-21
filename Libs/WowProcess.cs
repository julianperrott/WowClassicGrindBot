using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using TextCopy;

namespace Libs
{
    public class WowProcess : IRectProvider, IMouseInput
    {
        private readonly ILogger logger;
        private readonly IInput nativeInput;
        private readonly IInput simulatorInput;

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

        private readonly Dictionary<ConsoleKey, bool> keyDict = new Dictionary<ConsoleKey, bool>();

        public WowProcess(ILogger logger)
        {
            this.logger = logger;

            var process = Get();
            if (process == null)
            {
                throw new ArgumentOutOfRangeException("Unable to find the Wow process");
            }

            this._warcraftProcess = process;

            this.nativeInput = new InputWindowsNative(process);
            this.simulatorInput = new InputSimulator(process);
        }

        public bool IsWowClassic()
        {
            return WarcraftProcess.ProcessName.ToLower().Contains("classic");
        }

        //Get the wow-process, if success returns the process else null
        public static Process? Get(string name = "")
        {
            var names = string.IsNullOrEmpty(name) ? new List<string> { "Wow", "WowClassic", "WowClassicT", "Wow-64", "WowClassicB" } : new List<string> { name };

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

            if (!string.IsNullOrEmpty(description))
                Log($"KeyDown {key} " + description);

            nativeInput.KeyDown((int)key);
            keyDict[key] = true;
        }

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
            nativeInput.KeyUp((int)key);

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

        public async Task SendText(string payload)
        {
            await simulatorInput.SendText(payload);
        }

#pragma warning disable CA1822 // Mark members as static
        public void SetClipboard(string text)
#pragma warning restore CA1822 // Mark members as static
        {
            ClipboardService.SetText(text);
        }

        public void PasteFromClipboard()
        {
            simulatorInput.PasteFromClipboard();
        }

        public async Task KeyPress(ConsoleKey key, int milliseconds, string description = "")
        {
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            Log($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            await nativeInput.KeyPress((int)key, milliseconds);
        }

        public void KeyPressSleep(ConsoleKey key, int milliseconds, string description = "")
        {
            if (milliseconds < 1) { return; }
            var keyDescription = string.Empty;
            if (!string.IsNullOrEmpty(description)) { keyDescription = $"{description} "; }
            Log($"{keyDescription}[{key}] pressing for {milliseconds}ms");

            nativeInput.KeyPressSleep((int)key, milliseconds);
        }

        public void SetKeyState(ConsoleKey key, bool pressDown, bool forceClick, string description = "")
        {
            if (!string.IsNullOrEmpty(description))
                Log($"SetKeyState: {description}");

            if (pressDown) { KeyDown(key, description); } else { KeyUp(key, forceClick); }
        }

        public void SetCursorPosition(Point position)
        {
            nativeInput.SetCursorPosition(position);
        }

        public async Task RightClickMouse(Point position)
        {
            await nativeInput.RightClickMouse(position);
        }

        public async Task LeftClickMouse(Point position)
        {
            await nativeInput.LeftClickMouse(position);
        }

        public void GetWindowRect(out Rectangle rect)
        {
            NativeMethods.GetWindowRect(WarcraftProcess.MainWindowHandle, out rect);
        }

        private void Log(string text)
        {
            if (debug)
                logger.LogInformation($"{this.GetType().Name}: {text}");
        }
    }
}