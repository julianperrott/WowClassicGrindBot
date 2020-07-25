using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Libs.Cursor
{
    public static class CursorClassifier
    {
        private static Dictionary<CursorClassification, List<ulong>> imageHashes = new Dictionary<CursorClassification, List<ulong>>()
        {
            {CursorClassification.Kill, new List<ulong>{ 9286546093378506253 } },

            {CursorClassification.Loot, new List<ulong>{16205332705670085656}},

            {CursorClassification.Skin, new List<ulong>{13901748381153107456}},

            {CursorClassification.Mine,new List<ulong>{ 4669700909741929478,4669700909674820614 }},

            {CursorClassification.Herb,new List<ulong>{ 4683320813727784960,4669700909741929478,4683461550142398464 }},

            {CursorClassification.None,new List<ulong>{4645529528554094592, 4665762466636896256,6376251547633783040,6376251547633783552 }},

            {CursorClassification.Vendor,new List<ulong>{ 4645529528554094592, 17940331276560775168, 17940331276594329600,17940331276594460672} },
        };

        public static Bitmap Classify(out CursorClassification classification)
        {
            var result = new Bitmap(32, 32);
            try
            {
                NativeMethods.CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(NativeMethods.CURSORINFO));

                using (var g = Graphics.FromImage(result))
                {
                    if (NativeMethods.GetCursorInfo(out pci))
                    {
                        if (pci.flags == NativeMethods.CURSOR_SHOWING)
                        {
                            var hdc = g.GetHdc();
                            NativeMethods.DrawIconEx(hdc, 0, 0, pci.hCursor, 0, 0, 0, IntPtr.Zero, NativeMethods.DI_NORMAL);
                            g.ReleaseHdc();
                        }
                    }
                }

                var hash = ImageHashing.AverageHash(result);

                //var filename = hash + ".bmp";
                //if (!File.Exists(filename))
                //{
                //    result.Save(filename);
                //}

                var matching = imageHashes.SelectMany(i => i.Value.Select(v=> (similarity: ImageHashing.Similarity(hash, v), imagehash: i)))
                    .Where(t => t.similarity > 80)
                    .OrderByDescending(t => t.similarity)
                    .FirstOrDefault();

                classification = matching.imagehash.Key;
                Debug.WriteLine(classification.ToString() + " " + matching.similarity);

                if (classification == 0)
                {
                    classification = CursorClassification.None;
                }

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