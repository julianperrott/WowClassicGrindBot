using PInvoke;
using System;
using System.Runtime.InteropServices;

namespace Libs
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        public const Int32 CURSOR_SHOWING = 0x0001;
        public const Int32 DI_NORMAL = 0x0003;

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const UInt32 WM_LBUTTONDOWN = 0x201;
        public const UInt32 WM_LBUTTONUP = 0x202;
        public const UInt32 WM_RBUTTONDOWN = 0x204;
        public const UInt32 WM_RBUTTONUP = 0x205;
        public const int VK_RMB = 0x02;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public const int KEYEVENTF_KEYDOWN = 0x0000; // New definition
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_LCONTROL = 0xA2; //Left Control key code

        public const int VK_LEFT_SHIFT = 160;
        public const int VK_LEFT_CONTROL = 162;
        public const int VK_LEFT_ALT = 164;

        public const int A = 0x41; //A key code
        public const int C = 0x43; //C key code
    }
}