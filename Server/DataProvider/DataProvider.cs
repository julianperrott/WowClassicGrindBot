using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SharedLib;

namespace Server
{
    public class DataProvider : IDataProvider
    {
        private readonly ILogger logger;
        private readonly WowScreen wowScreen;
        private readonly DataFrame[] frames;

        private readonly DirectBitmapCapturer capturer;

        private readonly int width;
        private readonly int height;
        private Rectangle rectangle;


        private readonly Color[] FrameColor;
        private byte[] bytes;

        private readonly Thread thread;

        private bool enabled;
        public bool Enabled { get => enabled; set { enabled = value; } }

        public DataProvider(ILogger logger, WowScreen wowScreen, List<DataFrame> frames)
        {
            this.logger = logger;
            this.wowScreen = wowScreen;

            this.frames = frames.ToArray();
            FrameColor = new Color[frames.Count];

            this.width = frames.Last().point.X + 1;
            this.height = frames.Max(f => f.point.Y) + 1;

            bytes = new byte[FrameColor.Length * 3];

            wowScreen.GetRectangle(out rectangle);
            rectangle.Width = width;
            rectangle.Height = height;
            rectangle = new Rectangle(0, 0, width, height);
            capturer = new DirectBitmapCapturer(rectangle);

            Enabled = true;
            thread = new Thread(() => Update());
            thread.Start();
        }

        private void Update()
        {
            while(Enabled)
            {
                wowScreen.GetPosition(out var p);
                rectangle.X = p.X;
                rectangle.Y = p.Y;
                capturer.Capture(rectangle);

                for (int i = 0; i < frames.Length; i++)
                {
                    FrameColor[frames[i].index] = capturer.GetColorAt(frames[i].point);
                }
            }
        }

        public byte[] GetData()
        {
            for(int i = 0; i<FrameColor.Length; i++)
            {
                bytes[3 * i + 0] = FrameColor[i].R;
                bytes[3 * i + 1] = FrameColor[i].G;
                bytes[3 * i + 2] = FrameColor[i].B;
            }

            return bytes;
        }

        public bool HasData()
        {
            return Enabled;
        }
    }
}
