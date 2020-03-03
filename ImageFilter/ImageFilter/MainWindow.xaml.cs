using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            timer.AutoReset = false;
            timer.Enabled = true;
        }

        private System.Timers.Timer? timer;

        Bitmap savedImage = new Bitmap(System.Drawing.Image.FromFile(@"D:\GitHub\WowPixelBot\ImageFilter\6.png"));

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Font drawFont = new Font("Arial", 16);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var image = savedImage;
            //var image = new DirectBitmap(1920, 1080);

            var bitmap = new Bitmap(image.Width, image.Height);

            var aBrush = (System.Drawing.Brush)Brushes.Red;

            Debug.WriteLine("-----------------");
            using (var gr = Graphics.FromImage(bitmap))
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var lengthStart = -1;
                    var lengthEnd = -1;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var pixel = image.GetPixel(x, y);
                        if (pixel.R == 255 && pixel.G <= 55 && pixel.B <= 55)
                        {
                            var isSameSection = lengthStart > -1 && (x - lengthEnd) < 12;

                            if (isSameSection)
                            {
                                gr.FillRectangle(aBrush, lengthStart, y, x - lengthStart + 1, 1);
                                lengthEnd = x;
                            }
                            else
                            {
                                if (lengthStart > -1 && lengthEnd - lengthStart > 18)
                                {
                                    Debug.WriteLine($"Length: {lengthEnd - lengthStart} @ ({lengthStart},{y})");
                                    gr.DrawString( (lengthEnd - lengthStart).ToString(), drawFont, drawBrush, lengthEnd, y);
                                }

                                lengthStart = x;
                            }
                            lengthEnd = x;
                        }
                    }

                    if (lengthStart > -1 && lengthEnd - lengthStart > 18)
                    {
                        Debug.WriteLine($"Length: {lengthEnd - lengthStart} @ ({lengthStart},{y})");
                        gr.DrawString((lengthEnd - lengthStart).ToString(), drawFont, drawBrush, lengthEnd, y);
                    }
                }

            }

            stopwatch.Stop();

            Application.Current.Dispatcher.Invoke(new Action(() => { this.Screenshot.Source = image.ToBitmapImage(); }));
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
            width = (width * 2) / 3;
            height = height - 300;

            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());

            using (var graphics = Graphics.FromImage(Bitmap))
            {
                graphics.CopyFromScreen(width / 4, 100, 0, 0, Bitmap.Size);
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
