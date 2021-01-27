using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace Libs
{
    public class WowScreen : IColorReader
    {
        private readonly ILogger logger;

        public delegate void ScreenChangeEventHandler(object sender, ScreenChangeEventArgs args);

        public event ScreenChangeEventHandler? OnScreenChanged;

        private WowProcess wowProcess;

        public int Size { get; set; } = 1024;

        public WowScreen(WowProcess wowProcess, ILogger logger)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;
        }

        public Bitmap GetBitmap(int width, int height)
        {
            var bmpScreen = new Bitmap(width, height);
            var rect = Rectangle.Empty;

            NativeMethods.GetWindowRect(wowProcess.WarcraftProcess.MainWindowHandle, out rect);

            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, bmpScreen.Size);
            }
            return bmpScreen;
        }

        public Color GetColorAt(Point point, Bitmap bmp)
        {
            var color = bmp.GetPixel(point.X, point.Y);

            return color;
        }

        public void DoScreenshot(NpcNameFinder npcNameFinder)
        {
            try
            {
                var npcs = npcNameFinder.RefreshNpcPositions();
                var bitmap = npcNameFinder.Screenshot.Bitmap;

                using (var gr = Graphics.FromImage(bitmap))
                {
                    if (npcs.Count > 0)
                    {
                        var margin = 10;

                        using (var whitePen = new Pen(Color.White, 3))
                        {
                            npcs.ForEach(n => gr.DrawEllipse(whitePen, new Rectangle(n.ClickPoint.X - (margin / 2), n.ClickPoint.Y - (margin / 2), margin, margin)));
                        }
                    }
                    using (var blackPen = new SolidBrush(Color.Black))
                    {
                        gr.FillRectangle(blackPen, new Rectangle(new Point(bitmap.Width / 15, bitmap.Height / 40), new Size(bitmap.Width / 15, bitmap.Height / 40)));
                    }
                }

                this.OnScreenChanged?.Invoke(this, new ScreenChangeEventArgs(npcNameFinder.Screenshot.ToBase64(Size)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            Thread.Sleep(1);
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

        public static Bitmap GetCroppedMinimapBitmap(bool highlight)
        {
            return CropImage(GetMinimapBitmap(), highlight);
        }

        private static Bitmap GetMinimapBitmap()
        {
            int Width = 155;
            int Height = 155;
            int X = 1730;
            int Y = 38;

            var bmpScreen = new Bitmap(Width, Height);
            using (var graphics = Graphics.FromImage(bmpScreen))
            {
                graphics.CopyFromScreen(X, Y, 0, 0, bmpScreen.Size);
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
    }
}