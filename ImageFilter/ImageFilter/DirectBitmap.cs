using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace ImageFilter
{
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public int TopOffset { get; private set; } = 100;
        public int BottomOffset { get; private set; } = 200;

        protected GCHandle BitsHandle { get; private set; }

        public Point ToScreenCoordinates(int x, int y)
        {
            return new Point(x, y + 100);
        }

        public DirectBitmap(int width, int height)
        {
            this.Width = width;
            this.Height = height - TopOffset - BottomOffset;
            this.Bits = new Int32[width * height];
            this.BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            this.Bitmap = new Bitmap(width, Height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public void CaptureScreen()
        {
            using (var graphics = Graphics.FromImage(Bitmap))
            {
                graphics.CopyFromScreen(0, TopOffset, 0, 0, Bitmap.Size);
            }
        }

        public BitmapImage ToBitmapImage()
        {
            using (var memory = new MemoryStream())
            {
                Bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                memory.Dispose();
                return bitmapImage;
            }
        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();
            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }
}