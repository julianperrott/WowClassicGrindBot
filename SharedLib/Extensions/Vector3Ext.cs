using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharedLib.Extensions
{
    public static class Vector3Ext
    {
        public static List<Vector3> FromList(List<List<float>> points)
        {
            var output = new List<Vector3>();
            points.ForEach(p => output.Add(new Vector3(p[0], p[1], 0)));
            return output;
        }

        /*
        public static float DistanceTo(Vector3 l1, Vector3 l2)
        {
            var x = l1.X - l2.X;
            var y = l1.Y - l2.Y;
            x = x * 100;
            y = y * 100;
            float distance = MathF.Sqrt((x * x) + (y * y));

            //logger.LogInformation($"distance:{x} {y} {distance.ToString()}");
            return distance;
        }
        */

        public static float DistanceTo(this Vector3 l1, Vector3 l2)
        {
            var x = l1.X - l2.X;
            var y = l1.Y - l2.Y;
            x = x * 100;
            y = y * 100;
            float distance = MathF.Sqrt((x * x) + (y * y));

            //logger.LogInformation($"distance:{x} {y} {distance.ToString()}");
            return distance;
        }

        public static List<Vector3> ShortenRouteFromLocation(Vector3 location, List<Vector3> pointsList)
        {
            var result = new List<Vector3>();

            var closestDistance = pointsList.Select(p => (point: p, distance: DistanceTo(location, p)))
                .OrderBy(s => s.distance);

            var closestPoint = closestDistance.First();

            var startPoint = 0;
            for (int i = 0; i < pointsList.Count; i++)
            {
                if (pointsList[i] == closestPoint.point)
                {
                    startPoint = i;
                    break;
                }
            }

            for (int i = startPoint; i < pointsList.Count; i++)
            {
                result.Add(pointsList[i]);
            }

            return result;
        }


        public static Vector2 Vector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }
    }
}
