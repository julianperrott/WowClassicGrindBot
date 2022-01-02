using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SharedLib;
using Game;
using System.Drawing.Imaging;

namespace Core
{
    public sealed class AddonDataProvider : IAddonDataProvider
    {
        private readonly DataFrame[] frames;
        private readonly int[] data;

        private readonly IWowScreen wowScreen;

        private readonly Color firstColor = Color.FromArgb(255, 0, 0, 0);
        private readonly Color lastColor = Color.FromArgb(255, 30, 132, 129);

        private Rectangle rect;
        private Bitmap bitmap;

        public AddonDataProvider(IWowScreen wowScreen, List<DataFrame> frames)
        {
            this.wowScreen = wowScreen;

            this.frames = frames.ToArray();
            this.data = new int[this.frames.Length];

            rect.Width = frames.Last().point.X + 1;
            rect.Height = frames.Max(f => f.point.Y) + 1;

            bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppPArgb);
        }

        public void Update()
        {
            wowScreen.GetPosition(out Point p);
            rect.X = p.X;
            rect.Y = p.Y;

            bitmap.Dispose();
            bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, bitmap.Size);
            }

            if (!Visible()) return;

            Process();
        }

        private bool Visible()
        {
            return bitmap.GetPixel(frames[0].point.X, frames[0].point.Y) == firstColor &&
                bitmap.GetPixel(frames[^1].point.X, frames[^1].point.Y) == lastColor;
        }

        private void Process()
        {
            unsafe
            {
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                for (int i = 0; i < frames.Length; i++)
                {
                    byte* currentLine = (byte*)bitmapData.Scan0 + (frames[i].point.Y * bitmapData.Stride);
                    int x = frames[i].point.X * bytesPerPixel;

                    data[frames[i].index] = (currentLine[x + 2] * 65536) + (currentLine[x + 1] * 256) + currentLine[x];
                }
                bitmap.UnlockBits(bitmapData);
            }
        }

        public int GetInt(int index)
        {
            return data[index];
        }

        public void Dispose()
        {
            bitmap?.Dispose();
        }
    }
}
