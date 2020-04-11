using Microsoft.Extensions.Logging;
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
        private Color[] FrameColor { get; set; } = new Color[200];

        private readonly int width;
        private readonly int height;
        private readonly List<DataFrame> frames;
        private readonly IColorReader colorReader;
        public PlayerReader? PlayerReader { get; set; }
        private ILogger logger;

        public AddonReader(IColorReader colorReader, List<DataFrame> frames, int width, int height, ILogger logger)
        {
            this.colorReader = colorReader;
            this.frames = frames;
            this.width = width;
            this.height = height;
            this.logger = logger;
        }

        public void Refresh()
        {
            try
            {
                using (var bitMap = WowScreen.GetAddonBitmap(this.width, this.height))
                {
                    frames.ForEach(frame => FrameColor[frame.index] = colorReader.GetColorAt(frame.point, bitMap));
                }

                //logger.LogInformation(string.Join(",", frames.Select(frame => ToColor(frame, bitMap))));

                if (PlayerReader != null)
                {
                    PlayerReader.Updated();
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex.Message);
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