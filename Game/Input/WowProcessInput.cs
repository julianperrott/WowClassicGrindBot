using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using WinAPI;

namespace Game
{
    public partial class WowProcessInput : IMouseInput
    {
        private const int MIN_DELAY = 25;
        private const int MAX_DELAY = 55;

        private readonly ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly IInput nativeInput;
        private readonly IInput simulatorInput;

        private readonly ConcurrentDictionary<ConsoleKey, bool> keyDict = new();

        public WowProcessInput(ILogger logger, WowProcess wowProcess)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;

            this.nativeInput = new InputWindowsNative(wowProcess.WarcraftProcess, MIN_DELAY, MAX_DELAY);
            this.simulatorInput = new InputSimulator(wowProcess.WarcraftProcess, MIN_DELAY, MAX_DELAY);
        }

        private void KeyDown(ConsoleKey key, string description = "")
        {
            if (keyDict.ContainsKey(key))
            {
                //if (keyDict[key] == true) { return; }
            }
            else
            {
                if (!keyDict.TryAdd(key, true))
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(description))
                LogKeyDown(logger, key, description);

            nativeInput.KeyDown((int)key);
            keyDict[key] = true;
        }

        private void KeyUp(ConsoleKey key, bool forceClick, string description = "")
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
                if (!keyDict.TryAdd(key, false))
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(description))
                LogKeyUp(logger, key, description);

            nativeInput.KeyUp((int)key);

            keyDict[key] = false;
        }

        public bool IsKeyDown(ConsoleKey key)
        {
            if (keyDict.TryGetValue(key, out var down))
            {
                return down;
            }
            return false;
        }

        public void SendText(string payload)
        {
            simulatorInput.SendText(payload);
        }

        public void PasteFromClipboard()
        {
            simulatorInput.PasteFromClipboard();
        }

        public void SetForegroundWindow()
        {
            NativeMethods.SetForegroundWindow(wowProcess.WarcraftProcess.MainWindowHandle);
        }


        public void KeyPress(ConsoleKey key, int milliseconds, string description = "")
        {
            keyDict[key] = true;
            int totalElapsedMs = nativeInput.KeyPress((int)key, milliseconds);
            keyDict[key] = false;
            if (!string.IsNullOrEmpty(description))
            {
                LogKeyPress(logger, key, description, totalElapsedMs);
            }
        }

        public void KeyPressSleep(ConsoleKey key, int milliseconds, string description = "")
        {
            if (milliseconds < 1)
                return;

            if (!string.IsNullOrEmpty(description))
            {
                LogKeyPress(logger, key, description, milliseconds);
            }

            keyDict[key] = true;
            nativeInput.KeyPressSleep((int)key, milliseconds);
            keyDict[key] = false;
        }

        public void SetKeyState(ConsoleKey key, bool pressDown, bool forceClick, string description = "")
        {
            if (!string.IsNullOrEmpty(description))
                description = "SetKeyState-" + description;

            if (pressDown) { KeyDown(key, description); } else { KeyUp(key, forceClick, description); }
        }

        public void SetCursorPosition(Point position)
        {
            nativeInput.SetCursorPosition(position);
        }

        public void RightClickMouse(Point position)
        {
            nativeInput.RightClickMouse(position);
        }

        public void LeftClickMouse(Point position)
        {
            nativeInput.LeftClickMouse(position);
        }

        [LoggerMessage(
            EventId = 25,
            Level = LogLevel.Debug,
            Message = @"Input: KeyDown {key} {description}")]
        static partial void LogKeyDown(ILogger logger, ConsoleKey key, string description);

        [LoggerMessage(
            EventId = 26,
            Level = LogLevel.Debug,
            Message = @"Input: KeyUp {key} {description}")]
        static partial void LogKeyUp(ILogger logger, ConsoleKey key, string description);

        [LoggerMessage(
            EventId = 27,
            Level = LogLevel.Debug,
            Message = @"Input: [{key}] {description} pressed for {milliseconds}ms")]
        static partial void LogKeyPress(ILogger logger, ConsoleKey key, string description, int milliseconds);

        [LoggerMessage(
            EventId = 28,
            Level = LogLevel.Debug,
            Message = @"Input: [{key}] {description} pressing for {milliseconds}ms")]
        static partial void LogKeyPressNoDelay(ILogger logger, ConsoleKey key, string description, int milliseconds);
    }
}
