using System;
using System.Drawing;

namespace SharedLib
{
    public sealed class DirectBitmapCapturer : IDirectBitmapProvider, IBitmapProvider, IColorReader, IDisposable
    {
        private DirectBitmap directBitmap;
        public DirectBitmap DirectBitmap
        {
            get
            {
                return directBitmap;
            }
            set
            {
                directBitmap?.Dispose();
                directBitmap = value;
            }
        }

        private Rectangle rect;
        public Rectangle Rect
        {
            get => rect;

            set
            {
                directBitmap?.Dispose();
                rect = value;
            }
        }

        public Bitmap Bitmap => DirectBitmap.Bitmap;

        public DirectBitmapCapturer(Rectangle rect)
        {
            this.Rect = rect;
        }

        public void Capture()
        {
            DirectBitmap = new DirectBitmap(Rect);
            DirectBitmap.CaptureScreen();
        }
        public void Capture(Rectangle rect)
        {
            Rect = rect;
            DirectBitmap = new DirectBitmap(Rect);
            DirectBitmap.CaptureScreen();
        }

        public Color GetColorAt(Point point)
        {
            return DirectBitmap.GetPixel(point.X, point.Y);
        }

        public Bitmap GetBitmap(int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            Rectangle sourceRect = new Rectangle(0, 0, width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(DirectBitmap.Bitmap, 0, 0, sourceRect, GraphicsUnit.Pixel);
            }
            return bitmap;
        }

        public void Dispose()
        {
            directBitmap?.Dispose();
        }
    }
}
