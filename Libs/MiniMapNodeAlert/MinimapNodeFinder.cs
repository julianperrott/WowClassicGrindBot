using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

#nullable enable
namespace Libs
{
    public class MinimapNodeFinder : INodeFinder, IImageProvider
    {
        private readonly WowScreen wowScreen;
        private readonly IPixelClassifier pixelClassifier;

        private Bitmap bitmap = new Bitmap(1, 1);

        public event EventHandler<NodeEventArgs> NodeEvent;

        public MinimapNodeFinder(WowScreen wowScreen, IPixelClassifier pixelClassifier)
        {
            this.wowScreen = wowScreen;
            this.pixelClassifier = pixelClassifier;
            NodeEvent += (s, e) => { };
        }

        public Point? Find(bool highlight)
        {
            this.bitmap = wowScreen.GetCroppedMinimapBitmap(highlight);

            Score? best = Score.ScorePoints(FindYellowPoints());

            var e = new NodeEventArgs() { Bitmap = this.bitmap };
            if (best != null && best.count>2)
            {
                e.Point = best.point;
            }

            NodeEvent?.Invoke(this, e);

            this.bitmap.Dispose();

            return best?.point;
        }

        private List<Score> FindYellowPoints()
        {
            var points = new List<Score>();

            var minX = Math.Max(0, 0);
            var maxX = Math.Min(this.bitmap.Width, this.bitmap.Width);
            var minY = Math.Max(0, 0);
            var maxY = Math.Min(this.bitmap.Height, this.bitmap.Height);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    ProcessPixel(points, x, y);
                }
            }
            sw.Stop();

            if (points.Count > 100)
            {
                System.Diagnostics.Debug.WriteLine("Error: Too much yellow in this image, adjust the configuration !");
                points.Clear();
            }

            return points;
        }

        private void ProcessPixel(List<Score> points, int x, int y)
        {
            var p = this.bitmap.GetPixel(x, y);

            bool isMatch = this.pixelClassifier.IsMatch(p.R, p.G, p.B);

            if (isMatch)
            {
                points.Add(new Score { point = new Point(x, y) });
                this.bitmap.SetPixel(x, y, Color.Red);
            }
        }

        private class Score
        {
            public Point point;
            public int count = 0;

            public static Score? ScorePoints(List<Score> points)
            {
                var size = 5;

                foreach (Score p in points)
                {
                    p.count = points.Where(s => Math.Abs(s.point.X - p.point.X) < size) // + or - n pixels horizontally
                        .Where(s => Math.Abs(s.point.Y - p.point.Y) < size) // + or - n pixels vertically
                        .Count();
                }

                return points.OrderByDescending(s => s.count).FirstOrDefault();
            }
        }
    }
}