using System.Collections.Generic;
using System.Linq;

namespace ImageFilter
{
    public class NPCFinder
    {
        public List<LineOfNpcName> npcNameLine { get; set; }
        public List<List<LineOfNpcName>> npcs { get; set; }

        public LineOfNpcName GetNpcs(DirectBitmap directImage)
        {
            PopulateLinesOfNpcNames(directImage);
            DetermineNpcs();
            return npcs.Count == 0 ? null : npcs.First().First();
        }

        private void DetermineNpcs()
        {
            npcs = new List<List<LineOfNpcName>>();
            for (int i = 0; i < npcNameLine.Count; i++)
            {
                var npcLine = this.npcNameLine[i];
                var group = new List<LineOfNpcName>() { npcLine };
                var lastY = npcLine.Y;

                if (!npcLine.IsInAgroup)
                {
                    for (int j = i + 1; j < this.npcNameLine.Count; j++)
                    {
                        var laterNpcLine = this.npcNameLine[j];
                        if (laterNpcLine.Y > npcLine.Y + 10) { break; }
                        if (laterNpcLine.Y > lastY + 2) { break; }

                        if (laterNpcLine.XStart <= npcLine.X && laterNpcLine.XEnd >= npcLine.X && laterNpcLine.Y > lastY)
                        {
                            laterNpcLine.IsInAgroup = true;
                            group.Add(laterNpcLine);
                            lastY = laterNpcLine.Y;
                        }
                    }
                    if (group.Count > 1) { npcs.Add(group); }
                }
            }
        }

        private void PopulateLinesOfNpcNames(DirectBitmap directImage)
        {
            npcNameLine = new List<LineOfNpcName>();

            bool isEndOfSection;
            for (int y = 0; y < directImage.Height; y++)
            {
                var lengthStart = -1;
                var lengthEnd = -1;
                for (int x = 0; x < directImage.Width; x++)
                {
                    var pixel = directImage.GetPixel(x, y);
                    var isRedPixel = pixel.R > 200 && pixel.G <= 55 && pixel.B <= 55;

                    if (isRedPixel)
                    {
                        var isSameSection = lengthStart > -1 && (x - lengthEnd) < 22;

                        if (isSameSection)
                        {
                            lengthEnd = x;
                        }
                        else
                        {
                            isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > 18;

                            if (isEndOfSection)
                            {
                                npcNameLine.Add(new LineOfNpcName(lengthStart, lengthEnd, y));
                            }

                            lengthStart = x;
                        }
                        lengthEnd = x;
                    }
                }

                isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > 18;
                if (isEndOfSection)
                {
                    npcNameLine.Add(new LineOfNpcName(lengthStart, lengthEnd, y));
                }
            }
        }
    }
}