using Microsoft.Extensions.Logging;
using SharedLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace Core
{
    public sealed class WowScreen : IColorReader, IRectProvider, IDirectBitmapProvider, IDisposable
    {
        private readonly ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly DirectBitmapCapturer capturer;

        public delegate void ScreenChangeEventHandler(object sender, ScreenChangeEventArgs args);
        public event ScreenChangeEventHandler? OnScreenChanged;

        private List<Action<Graphics>> drawActions = new List<Action<Graphics>>();

        public int Size { get; set; } = 1024;

        private const int tick = 150;
        private DateTime lastScreenshot = default;

        public DirectBitmap DirectBitmap
        {
            get
            {
                return capturer.DirectBitmap;
            }
        }

        public WowScreen(ILogger logger, WowProcess wowProcess)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;

            GetRectangle(out var rect);
            this.capturer = new DirectBitmapCapturer(rect);
        }

        public void ForceScreenshot()
        {
            lastScreenshot = default;
        }

        public bool UpdateScreenshot()
        {
            if ((DateTime.Now - lastScreenshot).TotalMilliseconds > tick)
            {
                GetRectangle(out var rect);
                capturer.Capture(rect);
                return true;
            }

            return false;
        }

        public void GetPosition(out Point point)
        {
            NativeMethods.GetPosition(wowProcess.WarcraftProcess.MainWindowHandle, out point);
        }

        public void GetRectangle(out Rectangle rect)
        {
            NativeMethods.GetWindowRect(wowProcess.WarcraftProcess.MainWindowHandle, out rect);
        }


        public Bitmap GetBitmap(int width, int height)
        {
            var bmpScreen = new Bitmap(width, height);
            GetRectangle(out var rect);

            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, bmpScreen.Size);
            }
            return bmpScreen;
        }

        public Color GetColorAt(Point point)
        {
            return DirectBitmap.GetPixel(point.X, point.Y);
        }

        public static Color GetColorAt(Point point, Bitmap bmp)
        {
            return bmp.GetPixel(point.X, point.Y);
        }

        public void PostProcess()
        {
            var bitmap = DirectBitmap.Bitmap;
            using (var gr = Graphics.FromImage(bitmap))
            {
                using (var blackPen = new SolidBrush(Color.Black))
                {
                    gr.FillRectangle(blackPen, new Rectangle(new Point(bitmap.Width / 15, bitmap.Height / 40), new Size(bitmap.Width / 15, bitmap.Height / 40)));
                }

                drawActions.ForEach(x => x(gr));
            }

            this.OnScreenChanged?.Invoke(this, new ScreenChangeEventArgs(DirectBitmap.ToBase64(Size)));
        }

        public void AddDrawAction(Action<Graphics> a)
        {
            drawActions.Add(a);
        }

        private static Bitmap CropImage(Bitmap img, bool highlight)
        {
            int x = img.Width / 2;
            int y = img.Height / 2;
            int r = Math.Min(x, y);

            var tmp = new Bitmap(2 * r, 2 * r);
            using (Graphics g = Graphics.FromImage(tmp))
            {
                if (highlight)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 0, 0)))
                    {
                        g.FillRectangle(brush, 0, 0, img.Width, img.Height);
                    }
                }

                g.SmoothingMode = SmoothingMode.None;
                g.TranslateTransform(tmp.Width / 2, tmp.Height / 2);
                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(0 - r, 0 - r, 2 * r, 2 * r);
                    using (var region = new Region(gp))
                    {
                        g.SetClip(region, CombineMode.Replace);
                        using (var bmp = new Bitmap(img))
                        {

                            g.DrawImage(bmp, new Rectangle(-r, -r, 2 * r, 2 * r), new Rectangle(x - r, y - r, 2 * r, 2 * r), GraphicsUnit.Pixel);
                        }
                    }
                }
            }
            return tmp;
        }

        public Bitmap GetCroppedMinimapBitmap(bool highlight)
        {
            return CropImage(GetMinimapBitmap(), highlight);
        }

        private Bitmap GetMinimapBitmap()
        {
            GetRectangle(out var rect);

            int Size = 200;
            var bmpScreen = new Bitmap(Size, Size);
            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(rect.Right - Size, rect.Top, 0, 0, bmpScreen.Size);
            }
            return bmpScreen;
        }

        public static string ToBase64(Bitmap bitmap,int size)
        {
            int width, height;
            if (bitmap.Width > bitmap.Height)
            {
                width = size;
                height = Convert.ToInt32(bitmap.Height * size / (double)bitmap.Width);
            }
            else
            {
                width = Convert.ToInt32(bitmap.Width * size / (double)bitmap.Height);
                height = size;
            }
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(bitmap, 0, 0, width, height);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                resized.Save(ms, ImageFormat.Jpeg);
                resized.Dispose();
                byte[] byteImage = ms.ToArray();
                return Convert.ToBase64String(byteImage); // Get Base64
            }
        }

        public void Dispose()
        {
            capturer?.Dispose();
        }
    }
}