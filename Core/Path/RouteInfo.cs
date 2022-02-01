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

        public Stack<Vector3>? RouteToWaypoint
        {
            get
            {
                if (pathedRoutes.Any(x => x.HasNext()))
                {
                    return pathedRoutes.Select(r => r.PathingRoute()).First();
                }
                return default;
            }
        }

        private List<IRouteProvider> pathedRoutes = new List<IRouteProvider>();
        private readonly AddonReader addonReader;

        public List<RouteInfoPoi> PoiList { get; } = new List<RouteInfoPoi>();

        private double min;
        private double diff;

        private double addY;
        private double addX;

        private int margin;
        private int canvasSize;

        private double pointToGrid;

        private int dSize = 2;
        public void SetMargin(int margin)
        {
            this.margin = margin;
            CalculatePointToGrid();
        }

        public void SetCanvasSize(int size)
        {
            this.canvasSize = size;
            CalculatePointToGrid();
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

        public double DistanceToGrid(int value)
        {
            return value / 100f * pointToGrid;
        }

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
            var allPoints = this.PathPoints.ToList();

            if (SpiritPath.Count > 1)
                allPoints.AddRange(this.SpiritPath);

            var wayPoints = RouteToWaypoint;
            if (wayPoints != null)
                allPoints.AddRange(wayPoints);

            var pois = this.PoiList.Select(p => p.Location);
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
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < path.Count - 1; i++)
            {
                var pt1 = path[i];
                var pt2 = path[i + 1];
                sb.AppendLine($"<line x1 = '{ToCanvasPointX(pt1.X)}' y1 = '{ToCanvasPointY(pt1.Y)}' x2 = '{ToCanvasPointX(pt2.X)}' y2 = '{ToCanvasPointY(pt2.Y)}' />");
            }
            return sb.ToString();
        }

        private readonly string first = "<br><b>First</b>";
        private readonly string last = "<br><b>Last</b>";

        public string RenderPathPoints(List<Vector3> path)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                var wowpoint = path[i];
                float x = wowpoint.X;
                float y = wowpoint.Y;
                sb.AppendLine($"<circle onmousedown=\"pointClick(evt,{x},{y},{i});\"  onmousemove=\"showTooltip(evt,'{x},{y}{(i == 0 ? first : i == path.Count-1 ? last : string.Empty)}');\" onmouseout=\"hideTooltip();\"  cx = '{ToCanvasPointX(wowpoint.X)}' cy = '{ToCanvasPointY(wowpoint.Y)}' r = '{dSize}' />");
            }
            return sb.ToString();
        }

        public Vector3 NextPoint()
        {
            var route = this.pathedRoutes.OrderByDescending(s => s.LastActive).FirstOrDefault();
            if (route == null || !route.HasNext()) { return Vector3.Zero; }
            return route.NextPoint();
        }

        public string RenderNextPoint()
        {
            var pt = NextPoint();
            if (pt == Vector3.Zero) { return string.Empty; }
            return $"<circle cx = '{ToCanvasPointX(pt.X)}' cy = '{ToCanvasPointY(pt.Y)}'r = '{dSize + 1}' />";
        }

        public string DeathImage(Vector3 pt)
        {
            var size = this.canvasSize / 25;
            return pt == Vector3.Zero ? string.Empty : $"<image href = 'death.svg' x = '{ToCanvasPointX(pt.X) - size / 2}' y = '{ToCanvasPointY(pt.Y) - size / 2}' height='{size}' width='{size}' />";
        }

        public string DrawPoi(RouteInfoPoi poi)
        {
            return $"<circle onmousemove=\"showTooltip(evt, '{poi.Name}<br/>{poi.Location.X},{poi.Location.Y}');\" onmouseout=\"hideTooltip();\" cx='{ToCanvasPointX(poi.Location.X)}' cy='{ToCanvasPointY(poi.Location.Y)}' r='{(poi.Radius == 1 ? dSize : DistanceToGrid((int)poi.Radius))}' " + (poi.Radius == 1 ? $"fill='{poi.Color}'" : $"stroke='{poi.Color}' stroke-width='1' fill='none'") + " />";
        }
    }
}