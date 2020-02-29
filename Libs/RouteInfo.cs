using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace Libs
{
    public class RouteInfo
    {
        public List<WowPoint> PathPoints { get; private set; }
        public List<WowPoint> SpiritPath { get; private set; }

        private double min;
        private double diff;

        private double addY;
        private double addX;

        private int margin=0;
        private int canvasSize = 0;
        
        public void SetMargin(int margin)
        {
            this.margin = margin;
            CalculatePointToGrid();
        }
        public void SetCanvasSize(int size)
        {
            this.canvasSize = size;
            CalculatePointToGrid();

            StringBuilder sb = new StringBuilder();
            foreach (WowPoint point in PathPoints)
            {
                sb.AppendLine(point.X + "," + point.Y + "," + ToCanvasPointX(point.X) + "," + ToCanvasPointY(point.Y));
            }
            File.WriteAllText(@"out.csv", sb.ToString());
        }

        public void CalculatePointToGrid()
        {
            pointToGrid = ((double)canvasSize - (margin * 2)) / diff;
        }

        public int ToCanvasPointX(double value)
        {
            return (int)(margin + ((value +addX - min) * pointToGrid));
        }

        public int ToCanvasPointY(double value)
        {
            return (int)(margin + ((value+ addY- min) * pointToGrid));
        }

        private double pointToGrid;

        public RouteInfo(List<WowPoint> pathPoints, List<WowPoint> spiritPath)
        {
            this.PathPoints = pathPoints.ToList();
            this.SpiritPath = spiritPath.ToList();

            var maxX = this.PathPoints.Max(s=>s.X);
            var minX = this.PathPoints.Min(s => s.X);
            var diffX = maxX - minX;

            var maxY = this.PathPoints.Max(s => s.Y);
            var minY = this.PathPoints.Min(s => s.Y);
            var diffY = maxY - minY;

            this.addY = 0;
            this.addX = 0;

            if (diffX > diffY)
            {
                this.addY = minX - minY;
                this.min = minX;
                this.diff = diffX;
            }
            else
            {
                this.addX = minY - minX;
                this.min = minY;
                this.diff = diffY;
            }
        }
    }
}
