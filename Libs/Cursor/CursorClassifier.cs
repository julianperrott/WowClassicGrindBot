using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Libs.Cursor
{
    public enum CursorClassification
    {
        Kill = 10,
        Loot = 20,
        None = 30,
        Skin = 40,
        Mine = 50,
        Herb = 60,
        Unknown = 50
    }

    public class CursorClassifier
    {
        private static Dictionary<CursorClassification, ulong> imageHashes = new Dictionary<CursorClassification, ulong>()
        {
            {CursorClassification.Kill, 9286546093378506253},
            {CursorClassification.Loot, 16205332705670085656},
            {CursorClassification.None, 4645529528554094592},
            {CursorClassification.Skin, 13901748381153107456},
            {CursorClassification.Mine, 4669700909741929478 },
            {CursorClassification.Herb, 4683320813727784960 }
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        private const Int32 CURSOR_SHOWING = 0x0001;
        private const Int32 DI_NORMAL = 0x0003;


        public static Bitmap Classify(out CursorClassification classification)
        {
            var result = new Bitmap(32, 32);
            try
            {
                CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
               
                using (var g = Graphics.FromImage(result))
                {
                    if (GetCursorInfo(out pci))
                    {
                        if (pci.flags == CURSOR_SHOWING)
                        {
                            var hdc = g.GetHdc();
                            DrawIconEx(hdc, 0, 0, pci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                            g.ReleaseHdc();
                        }
                    }
                }

                var hash = ImageHashing.AverageHash(result);
                //logger.LogInformation("Hash: " + hash);

                var matching = imageHashes.Select(i => (similarity: ImageHashing.Similarity(hash, i.Value), imagehash: i))
                    .OrderByDescending(t => t.similarity)
                    .First();

                classification = matching.imagehash.Key;
                //System.Diagnostics.logger.LogInformation(classification);
                return result;
            }
            catch
            {
                classification = CursorClassification.Unknown;
                return result;
            }
        }
    }
}