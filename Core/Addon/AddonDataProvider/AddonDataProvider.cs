using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SharedLib;
using Game;

namespace Core
{
    public sealed class AddonDataProvider : IAddonDataProvider
    {
        private readonly DataFrame[] frames;
        private readonly Color[] FrameColor;

        private readonly IWowScreen wowScreen;
        private readonly int width;
        private readonly int height;
        private readonly DirectBitmapCapturer capturer;
        private Rectangle rectangle;

        public AddonDataProvider(IWowScreen wowScreen, List<DataFrame> frames)
        {
            this.wowScreen = wowScreen;

            this.frames = frames.ToArray();
            this.FrameColor = new Color[this.frames.Length];

            this.width = frames.Last().point.X + 1;
            this.height = frames.Max(f => f.point.Y) + 1;

            wowScreen.GetRectangle(out rectangle);
            rectangle.Width = width;
            rectangle.Height = height;
            rectangle = new Rectangle(0, 0, width, height);
            capturer = new DirectBitmapCapturer(rectangle);
        }

        public void Update()
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


        public Color GetColor(int index)
        {
            return FrameColor[index];
        }

        public void Dispose()
        {
            capturer?.Dispose();
        }
    }
}
