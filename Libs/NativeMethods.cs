using PInvoke;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

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
        
        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);

        public const Int32 CURSOR_SHOWING = 0x0001;
        public const Int32 DI_NORMAL = 0x0003;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

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
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

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

        // should be calculated from the metrics
        public const int WindowBarHeight = 31;
        public const int WindowBorderThick = 8;

        private static bool IsWindowedMode(Rectangle rect)
        {
            return rect.X != 0 || rect.Y != 0;
        }

        private static void GetNativeWindowRect(IntPtr hWnd, out Rectangle rect)
        {
            RECT nRect = new RECT();
            if (GetWindowRect(hWnd, ref nRect))
            {
                rect = new Rectangle
                {
                    X = nRect.left,
                    Y = nRect.top,
                    Width = (nRect.right - nRect.left),
                    Height = (nRect.bottom - nRect.top)
                };
            }
            else
            {
                rect = Rectangle.Empty;
            }
        }

        public static void GetWindowRect(IntPtr hWnd, out Rectangle rect)
        {
            GetNativeWindowRect(hWnd, out rect);

            if(IsWindowedMode(rect))
            {
                int border = WindowBorderThick;
                int header = WindowBarHeight;

                rect.Inflate(-border, 0);
                rect.Offset(0, header);
                rect.Height -= header;
            }
        }


        public static void SetClipboardText(string text)
        {
            OpenClipboard();

            EmptyClipboard();
            IntPtr hGlobal = default;
            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    ThrowWin32();
                }

                var target = GlobalLock(hGlobal);

                if (target == default)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                {
                    ThrowWin32();
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }

        public static void OpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        const uint cfUnicodeText = 13;

        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();
    }
}