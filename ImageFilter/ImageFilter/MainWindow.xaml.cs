using System;
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

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            timer = new System.Timers.Timer(3000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private System.Timers.Timer? timer;
        private DirectBitmap directImage;

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            directImage = new DirectBitmap(1920, 1080);
            directImage.CaptureScreen();
            var npcFinder = new NPCFinder();
            var npc = npcFinder.GetNpcs(directImage);

            stopwatch.Stop();

            Font drawFont = new Font("Arial", 24);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            var aBrush = (System.Drawing.Brush)Brushes.Red;

            var bitmap = new Bitmap(directImage.Width, directImage.Height);
            using (var gr = Graphics.FromImage(bitmap))
            {
                foreach (var item in npcFinder.npcNameLine)
                {
                    gr.FillRectangle(aBrush, item.XStart, item.Y, item.Length, 1);
                }
            }

            using (var gr = Graphics.FromImage(bitmap))
            {
                foreach (var group in npcFinder.npcs)
                {
                    var item = group.First();
                    gr.DrawString(group.Count().ToString(), drawFont, drawBrush, item.XStart, item.Y);
                }
            }

            if (npc != null)
            {
                var screenCoord = directImage.ToScreenCoordinates(npc.X, npc.Y + 35);
                SetCursorPosition(screenCoord);
            }

            Application.Current.Dispatcher.Invoke(new Action(() => { this.Screenshot.Source = directImage.ToBitmapImage(); }));
            Application.Current.Dispatcher.Invoke(new Action(() => { this.Screenshot2.Source = bitmap.ToBitmapImage(); }));
            Application.Current.Dispatcher.Invoke(new Action(() => { Duration.Content = "Duration: " + stopwatch.ElapsedMilliseconds + "ms"; }));
        }

        public void SetCursorPosition(System.Drawing.Point position)
        {
            SetCursorPos(position.X, position.Y);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

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
}