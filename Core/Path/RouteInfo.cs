using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using WowheadDB;

namespace Core
{
    public class RouteInfoPoi
    {
        public Vector3 Location { get; }
        public string Name { get; }
        public string Color { get; }

        public double Radius { get; }

        public RouteInfoPoi(NPC npc, string color)
        {
            Location = npc.points.First();
            Name = npc.name;
            Color = color;
            Radius = 1;
        }

        public RouteInfoPoi(Vector3 wowPoint, string name, string color, double radius)
        {
            Location = wowPoint;
            Name = name;
            Color = color;
            Radius = radius;
        }
    }

    public class RouteInfo
    {
        public List<Vector3> PathPoints { get; private set; }
        public List<Vector3> SpiritPath { get; private set; }

        public List<Vector3> RouteToWaypoint
        {
            get
            {
                var route = pathedRoutes.Select(r => r.PathingRoute())
                    .Where(r => r.Any())
                    .FirstOrDefault();

                return route ?? new List<Vector3>();
            }
        }

        private List<IRouteProvider> pathedRoutes = new List<IRouteProvider>();
        private readonly AddonReader addonReader;

        public List<RouteInfoPoi> PoiList { get; } = new List<RouteInfoPoi>();

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
            foreach (Vector3 point in PathPoints)
            {
                sb.AppendLine(point.X + "," + point.Y + "," + ToCanvasPointX(point.X) + "," + ToCanvasPointY(point.Y));
            }
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

        public RouteInfo(List<Vector3> pathPoints, List<Vector3> spiritPath, List<IRouteProvider> pathedRoutes, AddonReader addonReader)
        {
            this.PathPoints = pathPoints.ToList();
            this.SpiritPath = spiritPath.ToList();

            this.pathedRoutes = pathedRoutes;
            this.addonReader = addonReader;

            //addonReader.UIMapId.Changed -= OnZoneChanged;
            //addonReader.UIMapId.Changed += OnZoneChanged;
            //OnZoneChanged(this, EventArgs.Empty);

            CalculateDiffs();
        }

        private void OnZoneChanged(object sender, EventArgs e)
        {
            if (addonReader.AreaDb.CurrentArea != null)
            {
                PoiList.Clear();
                // Visualize the zone pois
                addonReader.AreaDb.CurrentArea.vendor?.ForEach(x => PoiList.Add(new RouteInfoPoi(x, "green")));
                addonReader.AreaDb.CurrentArea.repair?.ForEach(x => PoiList.Add(new RouteInfoPoi(x, "purple")));
                addonReader.AreaDb.CurrentArea.innkeeper?.ForEach(x => PoiList.Add(new RouteInfoPoi(x, "blue")));
                addonReader.AreaDb.CurrentArea.flightmaster?.ForEach(x => PoiList.Add(new RouteInfoPoi(x, "orange")));
            }
        }

        private void CalculateDiffs()
        {
            var allPoints = this.PathPoints.Select(s => s).ToList();
            allPoints.AddRange(this.SpiritPath);
            allPoints.AddRange(this.RouteToWaypoint);

            var pois = this.PoiList.Select(p => p.Location).ToList();
            allPoints.AddRange(pois);
            allPoints.Add(addonReader.PlayerReader.PlayerLocation);

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

        public string RenderPathLines(List<Vector3> path)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < path.Count - 1; i++)
            {
                var pt1 = path[i];
                var pt2 = path[i + 1];
                sb.AppendLine($"<line x1 = '{ToCanvasPointX(pt1.X)}' y1 = '{ToCanvasPointY(pt1.Y)}' x2 = '{ToCanvasPointX(pt2.X)}' y2 = '{ToCanvasPointY(pt2.Y)}' />");
            }
            return sb.ToString();
        }

        public string RenderPathPoints(List<Vector3> path)
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
            var route = this.pathedRoutes.OrderByDescending(s => s.LastActive).FirstOrDefault();
            if (route == null || !route.HasNext()) { return string.Empty; }
            var pt = route.NextPoint();
            return $"<circle cx = '{ToCanvasPointX(pt.X)}' cy = '{ToCanvasPointY(pt.Y)}'r = '3' />";
        }

        public string DeathImage(Vector3 pt)
        {
            var size = this.canvasSize / 25;
            return pt == null ? string.Empty : $"<image href = 'death.svg' x = '{ToCanvasPointX(pt.X) - size / 2}' y = '{ToCanvasPointY(pt.Y) - size / 2}' height='{size}' width='{size}' />";
        }

        public string DrawPoi(RouteInfoPoi pt)
        {
            var size = 4;
            return $"<circle onmousemove=\"showTooltip(evt, '{pt.Name}');\" onmouseout=\"hideTooltip();\" cx='{ToCanvasPointX(pt.Location.X) - size / 2}' cy='{ToCanvasPointY(pt.Location.Y) - size / 2}' " + (pt.Radius == 1 ? $"fill='{pt.Color}' r='{size}'" : $"r='{size * pt.Radius}' stroke='{pt.Color}' stroke-width='1' fill='none'") + " />";
        }
    }
}