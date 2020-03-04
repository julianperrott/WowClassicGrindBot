using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;

namespace ImageFilter
{
    public static class BitmapExtension
    {
        public static BitmapImage ToBitmapImage(this System.Drawing.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
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
    }

    public class NPCName
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int XEnd { get; set; }
        public bool IsInAgroup { get; set; } = false;

        public NPCName(int x, int xend, int y)
        {
            this.X = x;
            this.Y = y;
            this.XEnd = xend;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            timer = new System.Timers.Timer(5000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            var fileImage = new Bitmap(System.Drawing.Image.FromFile(@"C:\wip\WowPixelBot\ImageFilter\6.png"));
            directImage = new DirectBitmap(fileImage.Width, fileImage.Height);
            for (int y = 0; y < fileImage.Height; y++)
            {
                for (int x = 0; x < fileImage.Width; x++)
                {
                    directImage.SetPixel(x, y, fileImage.GetPixel(x, y));
                }
            }
        }

        private System.Timers.Timer? timer;
        private DirectBitmap directImage; 


        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            var npc = new List<NPCName>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            bool isEndOfSection;

            directImage = new DirectBitmap(1920, 1080);

            for (int y = 0; y < directImage.Height; y++)
            {
                var lengthStart = -1;
                var lengthEnd = -1;
                for (int x = 0; x < directImage.Width; x++)
                {
                    var pixel = directImage.GetPixel(x, y);
                    var isRedPixel = pixel.R == 255 && pixel.G <= 55 && pixel.B <= 55;

                    if (isRedPixel)
                    {
                        var isSameSection = lengthStart > -1 && (x - lengthEnd) < 12;

                        if (isSameSection)
                        {
                            lengthEnd = x;
                        }
                        else
                        {
                            isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > 18;

                            if (isEndOfSection)
                            {
                                npc.Add(new NPCName(lengthStart, lengthEnd, y));
                            }

                            lengthStart = x;
                        }
                        lengthEnd = x;
                    }
                }

                isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > 18;
                if (isEndOfSection)
                {
                    npc.Add(new NPCName(lengthStart, lengthEnd, y));
                }
            }

            stopwatch.Stop();

            Font drawFont = new Font("Arial", 24);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            var aBrush = (System.Drawing.Brush)Brushes.Red;

            var bitmap = new Bitmap(directImage.Width, directImage.Height);
            using (var gr = Graphics.FromImage(bitmap))
            {
                foreach (var item in npc)
                {
                    gr.FillRectangle(aBrush, item.X, item.Y, item.XEnd - item.X + 1, 1);
                }
            }

            var npcGroup = new List<List<NPCName>>();

            for (int i = 0; i < npc.Count; i++)
            {
                var mob = npc[i];
                var group = new List<NPCName>() { mob };
                var lastY = mob.Y;
                int testX = mob.X + ((mob.XEnd - mob.X + 1) / 2); // mid point

                if (!mob.IsInAgroup)
                {
                    for (int j = i + 1; j < npc.Count; j++)
                    {
                        var pMob = npc[j];
                        if (pMob.Y > mob.Y + 10) { break; }
                        if (pMob.Y > lastY + 2) { break; }

                        if (pMob.X <= testX && pMob.XEnd >= testX && pMob.Y > lastY)
                        {
                            pMob.IsInAgroup = true;
                            group.Add(pMob);
                            lastY = pMob.Y;
                        }
                    }
                    if (group.Count>1) { npcGroup.Add(group); }
                }
            }

            using (var gr = Graphics.FromImage(bitmap))
            {
                foreach (var group in npcGroup)
                {
                    var item = group.First();
                    gr.DrawString(group.Count().ToString(), drawFont, drawBrush, item.X, item.Y);
                }
            }

            Application.Current.Dispatcher.Invoke(new Action(() => { this.Screenshot.Source = directImage.ToBitmapImage(); }));
            Application.Current.Dispatcher.Invoke(new Action(() => { this.Screenshot2.Source = bitmap.ToBitmapImage(); }));
            Application.Current.Dispatcher.Invoke(new Action(() => { Duration.Content = "Duration: " + stopwatch.ElapsedMilliseconds + "ms"; }));
        }

        private int redwidth = 10;

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                redwidth = int.Parse((sender as TextBox).Text);
            }
            catch
            {
            }
        }
    }

    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            //width = (width * 2) / 3;
            //height = height - 300;

            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());

            using (var graphics = Graphics.FromImage(Bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, Bitmap.Size);
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
            try
            {


                int index = x + (y * Width);
                int col = colour.ToArgb();

                Bits[index] = col;
            }
            catch(Exception ex)
            {

            }
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