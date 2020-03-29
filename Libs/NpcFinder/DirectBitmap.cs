using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Libs.NpcFinder
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

        public DirectBitmap(int width, int height, int topOffset, int bottomOffset)
        {
            this.TopOffset = topOffset;
            this.BottomOffset = bottomOffset;
            this.Width = width;
            this.Height = height - TopOffset - BottomOffset;
            this.Bits = new Int32[width * height];
            this.BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            this.Bitmap = new Bitmap(width, Height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }


        public DirectBitmap(int width, int height)
        {
            this.Width = width;
            this.Height = height - TopOffset - BottomOffset;
            this.Bits = new Int32[width * height];
            this.BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            this.Bitmap = new Bitmap(width, Height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public string ToBase64()
        {
            const int size = 1024;
            //const int quality = 75;

            int width, height;
            if (Bitmap.Width > Bitmap.Height)
            {
                width = size;
                height = Convert.ToInt32(Bitmap.Height * size / (double)Bitmap.Width);
            }
            else
            {
                width = Convert.ToInt32(Bitmap.Width * size / (double)Bitmap.Height);
                height = size;
            }
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(Bitmap, 0, 0, width, height);

                //using (var output = new MemoryStream())
                //{
                //    var qualityParamId = Encoder.Quality;
                //    var encoderParameters = new EncoderParameters(1);
                //    encoderParameters.Param[0] = new EncoderParameter(qualityParamId, quality);
                //    var codec = ImageCodecInfo.GetImageDecoders()
                //        .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                //    resized.Save(output, codec, encoderParameters);

                //    byte[] byteImage = output.ToArray();
                //    return Convert.ToBase64String(byteImage); // Get Base64
                //}
            }

            using (System.IO.MemoryStream ms = new MemoryStream())
            {
                resized.Save(ms, ImageFormat.Jpeg);
                byte[] byteImage = ms.ToArray();
                return Convert.ToBase64String(byteImage); // Get Base64
            }
        }


        public void CaptureScreen()
        {
            using (var graphics = Graphics.FromImage(Bitmap))
            {
                graphics.CopyFromScreen(0, TopOffset, 0, 0, Bitmap.Size);
            }
        }

        //public BitmapImage ToBitmapImage()
        //{
        //    using (var memory = new MemoryStream())
        //    {
        //        Bitmap.Save(memory, ImageFormat.Png);
        //        memory.Position = 0;

        //        var bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = memory;
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.EndInit();
        //        bitmapImage.Freeze();

        //        memory.Dispose();
        //        return bitmapImage;
        //    }
        //}

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