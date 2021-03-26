using PInvoke;
using System;
using System.Drawing;
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
        
        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);

        public const Int32 CURSOR_SHOWING = 0x0001;
        public const Int32 DI_NORMAL = 0x0003;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

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

    }
}