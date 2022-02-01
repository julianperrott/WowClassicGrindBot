using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WinAPI;

namespace Core
{
    public static class CursorClassifier
    {
        private static readonly Dictionary<CursorType, List<ulong>> imageHashes = new Dictionary<CursorType, List<ulong>>()
        {
            {CursorType.Kill, new List<ulong>{ 9286546093378506253 } },

            {CursorType.Loot, new List<ulong>{16205332705670085656}},

            {CursorType.Skin, new List<ulong>{13901748381153107456}},

            {CursorType.Mine, new List<ulong>{ 4669700909741929478,4669700909674820614 }},

            {CursorType.Herb, new List<ulong>{ 4683320813727784960,4669700909741929478,4683461550142398464 }},

            {CursorType.None, new List<ulong>{4645529528554094592, 4665762466636896256,6376251547633783040,6376251547633783552 }},

            {CursorType.Vendor, new List<ulong>{ 17940331276560775168, 17940331276594329600,17940331276594460672} },

            {CursorType.Repair, new List<ulong>{ 16207573517913036808, 4669140166357294088 } },

            {CursorType.Innkeeper, new List<ulong> { 4667452417086599168, 4676529985085517824 } },

            {CursorType.Quest, new List<ulong> { 4682718988357606424, 4682718988358655000 } }
        };

        public static void Classify(out CursorType classification)
        {
            Size size = NativeMethods.GetCursorSize();
            Bitmap cursor = new Bitmap(size.Width, size.Height);

            var cursorInfo = new NativeMethods.CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
            if (NativeMethods.GetCursorInfo(out cursorInfo))
            {
                using Graphics g = Graphics.FromImage(cursor);
                if (cursorInfo.flags == NativeMethods.CURSOR_SHOWING)
                {
                    NativeMethods.DrawIcon(g.GetHdc(), 0, 0, cursorInfo.hCursor);
                }
            }

            var hash = ImageHashing.AverageHash(cursor);
            //var filename = hash + ".bmp";
            //var path = Path.Join("../Cursors/", filename);
            //if (!File.Exists(path))
            //{
            //    cursor.Save(path);
            //}
            cursor.Dispose();

            var matching = imageHashes
                .SelectMany(i => i.Value.Select(v => (similarity: ImageHashing.Similarity(hash, v), imagehash: i)))
                .Where(t => t.similarity > 80)
                .OrderByDescending(t => t.similarity)
                .FirstOrDefault();

            classification = matching.imagehash.Key;
            Debug.WriteLine($"[CursorClassifier.Classify] {classification} - {matching.similarity}");
        }
    }
}