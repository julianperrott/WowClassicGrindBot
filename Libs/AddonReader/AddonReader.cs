using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Libs
{
    public class AddonReader : IAddonReader
    {
        private Color[] FrameColor { get; set; } = new Color[100];

        private readonly int width;
        private readonly int height;
        private readonly List<DataFrame> frames;
        private readonly IColorReader colorReader;
        public PlayerReader? PlayerReader { get; set; }

        public AddonReader(IColorReader colorReader, List<DataFrame> frames, int width, int height)
        {
            this.colorReader = colorReader;
            this.frames = frames;
            this.width = width;
            this.height = height;
        }

        public void Refresh()
        {
            try
            {
                var bitMap = WowScreen.GetAddonBitmap(this.width, this.height);
                frames.ForEach(frame => FrameColor[frame.index] = colorReader.GetColorAt(frame.point, bitMap));

                //Debug.WriteLine(string.Join(",", frames.Select(frame => ToColor(frame, bitMap))));

                if (PlayerReader != null)
                {
                    PlayerReader.Updated();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                GC.Collect();
            }
        }

        private string ToColor(DataFrame frame, Bitmap bitMap)
        {
            var color= colorReader.GetColorAt(frame.point, bitMap);
            return BitConverter.ToString(new byte[] { color.R, color.G, color.B });
        }

        public Color GetColorAt(int index)
        {
            return FrameColor[index];
        }
    }
}