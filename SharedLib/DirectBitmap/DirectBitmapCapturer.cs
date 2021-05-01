using System;
using System.Drawing;

namespace SharedLib
{
    public sealed class DirectBitmapCapturer : IDirectBitmapProvider, IColorReader, IDisposable
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
            get 
            { 
                return rect; 
            }

            set
            {
                directBitmap?.Dispose();
                rect = value;
            }  
        }

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
            Bitmap b = new Bitmap(width, height);
            for(int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    b.SetPixel(x, y, DirectBitmap.GetPixel(x, y));
                }
            }
            return b;
        }

        public void Dispose()
        {
            directBitmap?.Dispose();
        }
    }
}
