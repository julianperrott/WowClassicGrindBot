using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Color = System.Drawing.Color;

using Serilog;
using Serilog.Extensions.Logging;
using SharedLib;
using SharedLib.NpcFinder;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;

namespace ImageFilter
{
    public partial class MainWindow : Window
    {
        private System.Timers.Timer? timer;
        private int redwidth = 10;

        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly DirectBitmapCapturer capturer;
        private readonly NpcNameFinder npcNameFinder;

        public MainWindow()
        {
            InitializeComponent();
            timer = new Timer(1000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            var logConfig = new LoggerConfiguration()
                //.WriteTo.File("names.log")
                .WriteTo.Debug()
                .CreateLogger();

            Log.Logger = logConfig;
            logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(MainWindow));

            var rect = new Rectangle(0, 0, 1920, 1080);
            capturer = new DirectBitmapCapturer(rect);

            npcNameFinder = new NpcNameFinder(logger, capturer);
            npcNameFinder.ChangeNpcType(NpcNames.Neutral | NpcNames.Friendly);

            InitSliders();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            capturer.Capture();

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            this.npcNameFinder.Update();
            stopwatch.Stop();
            //logger.LogInformation($"Update: {stopwatch.ElapsedMilliseconds}ms");

            var bitmap = new Bitmap(capturer.Rect.Width, capturer.Rect.Height);
            using (var gr = Graphics.FromImage(bitmap))
            {
                Font drawFont = new Font("Arial", 10);
                SolidBrush drawBrush = new SolidBrush(Color.White);

                if (npcNameFinder.Npcs.Count > 0)
                {
                    using (var whitePen = new Pen(Color.White, 1))
                    {
                        gr.DrawRectangle(whitePen, npcNameFinder.Area);

                        npcNameFinder.Npcs.ForEach(n =>
                        {
                            //npcNameTargeting.locTargetingAndClickNpc.ForEach(l =>
                            //{
                            //gr.DrawEllipse(whitePen, l.X + n.ClickPoint.X, l.Y + n.ClickPoint.Y, 5, 5);
                            gr.DrawEllipse(whitePen, n.ClickPoint.X, n.ClickPoint.Y, 5, 5);
                            //});
                        });


                        npcNameFinder.Npcs.ForEach(n => gr.DrawRectangle(whitePen, new Rectangle(n.Min, new System.Drawing.Size(n.Width, n.Height))));
                        npcNameFinder.Npcs.ForEach(n => gr.DrawString(npcNameFinder.Npcs.IndexOf(n).ToString(), drawFont, drawBrush, new PointF(n.Min.X - 20f, n.Min.Y)));
                    }
                }
            }

            npcNameFinder.Npcs.ForEach(n =>
            {
               //logger.LogInformation($"{npcNameFinder.Npcs.IndexOf(n),2} -> rect={new Rectangle(n.Min.X, n.Min.Y, n.Width, n.Height)} ClickPoint={{{n.ClickPoint.X,4},{n.ClickPoint.Y,4}}}");
            });

            //logger.LogInformation("\n");

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if(capturer != null)
                    this.Screenshot.Source = ToBitmapImage(capturer.DirectBitmap.Bitmap);

                this.Screenshot2.Source = ToBitmapImage(bitmap);
                Duration.Content = "Duration: " + stopwatch.ElapsedMilliseconds + "ms";

                bitmap.Dispose();
                bitmap = null;
            }));
        }

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


        public static BitmapImage ToBitmapImage(Bitmap Bitmap)
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


        #region Cursor Input 

        public static void SetCursorPosition(System.Drawing.Point position)
        {
            //SetCursorPos(position.X, position.Y);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        #endregion


        #region sliders

        private void InitSliders()
        {
            npcPosYOffset.Value = npcNameFinder.npcPosYOffset;
            lnpcPosYOffset.Content = "npcPosYOffset: " + npcNameFinder.npcPosYOffset;

            npcPosYHeightMul.Value = npcNameFinder.npcPosYHeightMul;
            lnpcPosYHeightMul.Content = "npcPosYHeightMul: " + npcNameFinder.npcPosYHeightMul;

            npcNameMaxWidth.Value = npcNameFinder.npcNameMaxWidth;
            lnpcNameMaxWidth.Content = "npcNameMaxWidth: " + npcNameFinder.npcNameMaxWidth;

            LinesOfNpcMinLength.Value = npcNameFinder.LinesOfNpcMinLength;
            lLinesOfNpcMinLength.Content = "LinesOfNpcMinLength: " + npcNameFinder.LinesOfNpcMinLength;

            LinesOfNpcLengthDiff.Value = npcNameFinder.LinesOfNpcLengthDiff;
            lLinesOfNpcLengthDiff.Content = "LinesOfNpcLengthDiff: " + npcNameFinder.LinesOfNpcLengthDiff;

            DetermineNpcsHeightOffset1.Value = npcNameFinder.DetermineNpcsHeightOffset1;
            lDetermineNpcsHeightOffset1.Content = "DetermineNpcsHeightOffset1: " + npcNameFinder.DetermineNpcsHeightOffset1;

            DetermineNpcsHeightOffset2.Value = npcNameFinder.DetermineNpcsHeightOffset2;
            lDetermineNpcsHeightOffset2.Content = "DetermineNpcsHeightOffset2: " + npcNameFinder.DetermineNpcsHeightOffset2;

            incX.Value = npcNameFinder.incX;
            lincX.Content = "incX: " + npcNameFinder.incX;

            incY.Value = npcNameFinder.incY;
            lincY.Content = "incY: " + npcNameFinder.incY;
        }

        public void npcPosYOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.npcPosYOffset = (int)e.NewValue;
            lnpcPosYOffset.Content = "npcPosYOffset: " + npcNameFinder.npcPosYOffset;
            //logger.LogInformation($"npcNameFinder.npcPosYOffset: {npcNameFinder.npcPosYOffset}");
        }

        public void npcPosYHeightMul_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.npcPosYHeightMul = (int)e.NewValue;
            lnpcPosYHeightMul.Content = "npcPosYHeightMul: " + npcNameFinder.npcPosYHeightMul;
            //logger.LogInformation($"npcNameFinder.npcPosYHeightMul: {npcNameFinder.npcPosYHeightMul}");
        }


        public void npcNameMaxWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.npcNameMaxWidth = (int)e.NewValue;
            lnpcNameMaxWidth.Content = "npcNameMaxWidth: " + npcNameFinder.npcNameMaxWidth;
            //logger.LogInformation($"npcNameFinder.npcNameMaxWidth: {npcNameFinder.npcNameMaxWidth}");
        }


        public void LinesOfNpcMinLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.LinesOfNpcMinLength = (int)e.NewValue;
            lLinesOfNpcMinLength.Content = "LinesOfNpcMinLength: " + npcNameFinder.LinesOfNpcMinLength;
            //logger.LogInformation($"npcNameFinder.LinesOfNpcMinLength: {npcNameFinder.LinesOfNpcMinLength}");
        }

        public void LinesOfNpcLengthDiff_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.LinesOfNpcLengthDiff = (int)e.NewValue;
            lLinesOfNpcLengthDiff.Content = "LinesOfNpcLengthDiff: " + npcNameFinder.LinesOfNpcLengthDiff;
            //logger.LogInformation($"npcNameFinder.LinesOfNpcLengthDiff: {npcNameFinder.LinesOfNpcLengthDiff}");
        }


        public void DetermineNpcsHeightOffset1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.DetermineNpcsHeightOffset1 = (int)e.NewValue;
            lDetermineNpcsHeightOffset1.Content = "DetermineNpcsHeightOffset1: " + npcNameFinder.DetermineNpcsHeightOffset1;
            //logger.LogInformation($"npcNameFinder.DetermineNpcsHeightOffset1: {npcNameFinder.DetermineNpcsHeightOffset1}");
        }

        public void DetermineNpcsHeightOffset2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.DetermineNpcsHeightOffset2 = (int)e.NewValue;
            lDetermineNpcsHeightOffset2.Content = "DetermineNpcsHeightOffset2: " + npcNameFinder.DetermineNpcsHeightOffset2;
            //logger.LogInformation($"npcNameFinder.DetermineNpcsHeightOffset2: {npcNameFinder.DetermineNpcsHeightOffset2}");
        }

        public void incX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.incX = (int)e.NewValue;
            lincX.Content = "incX: " + npcNameFinder.incX;
            //logger.LogInformation($"npcNameFinder.DetermineNpcsHeightOffset2: {npcNameFinder.DetermineNpcsHeightOffset2}");
        }

        public void incY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (npcNameFinder == null) return;

            npcNameFinder.incY = (int)e.NewValue;
            lincY.Content = "incY: " + npcNameFinder.incY;
            //logger.LogInformation($"npcNameFinder.DetermineNpcsHeightOffset2: {npcNameFinder.DetermineNpcsHeightOffset2}");
        }



        #endregion
    }
}