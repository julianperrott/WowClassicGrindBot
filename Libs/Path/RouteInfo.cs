using Libs.Actions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Libs
{
    public class RouteInfo
    {
        public List<WowPoint> PathPoints { get; private set; }
        public List<WowPoint> SpiritPath { get; private set; }
        private readonly FollowRouteAction followRouteAction;
        private readonly WalkToCorpseAction walkToCorpseAction;

        private double min;
        private double diff;

        private double addY;
        private double addX;

        private int margin = 0;
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
            CalculateDiffs();
        }

        public int ToCanvasPointX(double value)
        {
            return (int)(margin + ((value + addX - min) * pointToGrid));
        }

        public int ToCanvasPointY(double value)
        {
            return (int)(margin + ((value + addY - min) * pointToGrid));
        }

        private double pointToGrid;

        public RouteInfo(List<WowPoint> pathPoints, List<WowPoint> spiritPath, FollowRouteAction followRouteAction, WalkToCorpseAction walkToCorpseAction)
        {
            this.PathPoints = pathPoints.ToList();
            this.SpiritPath = spiritPath.ToList();

            this.followRouteAction = followRouteAction;
            this.walkToCorpseAction = walkToCorpseAction;

            CalculateDiffs();
        }

        private void CalculateDiffs()
        {
            var allPoints = this.PathPoints.Select(s => s).ToList();
            allPoints.AddRange(this.SpiritPath);

            var maxX = allPoints.Max(s => s.X);
            var minX = allPoints.Min(s => s.X);
            var diffX = maxX - minX;

            var maxY = allPoints.Max(s => s.Y);
            var minY = allPoints.Min(s => s.Y);
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

        public string RenderPathLines(List<WowPoint> path)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < path.Count() - 1; i++)
            {
                var pt1 = path[i];
                var pt2 = path[i + 1];
                sb.AppendLine($"<line x1 = '{ToCanvasPointX(pt1.X)}' y1 = '{ToCanvasPointY(pt1.Y)}' x2 = '{ToCanvasPointX(pt2.X)}' y2 = '{ToCanvasPointY(pt2.Y)}' />");
            }
            return sb.ToString();
        }

        public string RenderPathPoints(List<WowPoint> path)
        {
            var sb = new StringBuilder();

            foreach (var wowpoint in path)
            {
                var x = wowpoint.X.ToString("0.00");
                var y = wowpoint.Y.ToString("0.00");
                sb.AppendLine($"<circle  onmousemove=\"showTooltip(evt, '{x},{y}');\" onmouseout=\"hideTooltip();\"  cx = '{ToCanvasPointX(wowpoint.X)}' cy = '{ToCanvasPointY(wowpoint.Y)}' r = '2' />");
            }
            return sb.ToString();
        }

        public string NextPoint()
        {
            var pt = followRouteAction.LastActive > walkToCorpseAction.LastActive ? followRouteAction.NextPoint() : walkToCorpseAction.NextPoint();
            return pt == null ? string.Empty : $"<circle cx = '{ToCanvasPointX(pt.X)}' cy = '{ToCanvasPointY(pt.Y)}'r = '3' />";
        }

        public string DeathImage(WowPoint pt)
        {
            var size = this.canvasSize / 25;
            return pt == null ? string.Empty : $"<image href = 'death.svg' x = '{ToCanvasPointX(pt.X)-size/2}' y = '{ToCanvasPointY(pt.Y)- size / 2}' height='{size}' width='{size}' />";
        }
    }
}