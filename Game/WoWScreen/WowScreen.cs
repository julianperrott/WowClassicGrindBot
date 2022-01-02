using Microsoft.Extensions.Logging;
using SharedLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using WinAPI;

namespace Game
{
    public sealed class WowScreen : IWowScreen, IBitmapProvider, IDisposable
    {
        private readonly ILogger logger;
        private readonly WowProcess wowProcess;

        public delegate void ScreenChangeEventHandler(object sender, ScreenChangeEventArgs args);
        public event ScreenChangeEventHandler OnScreenChanged;

        private readonly List<Action<Graphics>> drawActions = new List<Action<Graphics>>();

        public int Size { get; set; } = 1024;

        public bool Enabled { get; set; } = true;

        public bool EnablePostProcess { get; set; } = true;

        private Bitmap bitmap1, bitmap2;
        private bool isBitmap1 = false;
        public Bitmap Bitmap
        {
            get
            {
                return isBitmap1 ? bitmap1 : bitmap2;
            }
            set
            {
                if (isBitmap1)
                {
                    bitmap2 = value;
                    if (bitmap1 != null) bitmap1.Dispose();
                }
                else
                {
                    bitmap1 = value;
                    if (bitmap2 != null) bitmap2.Dispose();
                }
                isBitmap1 = !isBitmap1;
            }
        }

        private Rectangle rect;
        public Rectangle Rect => rect;

        public WowScreen(ILogger logger, WowProcess wowProcess)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;
        }

        public void UpdateScreenshot()
        {
            GetPosition(out var p);
            GetRectangle(out rect);
            rect.X = p.X;
            rect.Y = p.Y;

            Bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(Bitmap))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, Bitmap.Size);
            }
        }

        public void AddDrawAction(Action<Graphics> a)
        {
            drawActions.Add(a);
        }

        public void PostProcess()
        {
            if (!EnablePostProcess)
                return;

            using (var gr = Graphics.FromImage(Bitmap))
            {
                using (var blackPen = new SolidBrush(Color.Black))
                {
                    gr.FillRectangle(blackPen, new Rectangle(new Point(Bitmap.Width / 15, Bitmap.Height / 40), new Size(Bitmap.Width / 15, Bitmap.Height / 40)));
                }

                drawActions.ForEach(x => x(gr));
            }

            this.OnScreenChanged?.Invoke(this, new ScreenChangeEventArgs(ToBase64(Bitmap, Size)));
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
            UpdateScreenshot();

            Bitmap bitmap = new Bitmap(width, height);
            Rectangle sourceRect = new Rectangle(0, 0, width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(Bitmap, 0, 0, sourceRect, GraphicsUnit.Pixel);
            }
            return bitmap;
        }

        public Color GetColorAt(Point point)
        {
            return Bitmap.GetPixel(point.X, point.Y);
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

        public void Dispose()
        {
            Bitmap?.Dispose();
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

        public static string ToBase64(Bitmap bitmap, int size)
        {
            int width, height;
            if (bitmap.Width > bitmap.Height)
            {
                width = size;
                height = Convert.ToInt32(bitmap.Height * size / (float)bitmap.Width);
            }
            else
            {
                width = Convert.ToInt32(bitmap.Width * size / (float)bitmap.Height);
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

    }
}