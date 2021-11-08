using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharedLib.Extensions
{
    public static class VectorExt
    {
        public static List<Vector3> FromList(List<List<float>> points)
        {
            var output = new List<Vector3>();
            points.ForEach(p => output.Add(new Vector3(p[0], p[1], 0)));
            return output;
        }

        public static float DistanceXYTo(this Vector3 l1, Vector3 l2)
        {
            float distance = Vector2.Distance(l1.AsVector2() * 100, l2.AsVector2() * 100);
            //float distance = Vector3.DistanceSquared(l1, l2);

            //float x = l1.X - l2.X;
            //float y = l1.Y - l2.Y;
            //x = x * 100;
            //y = y * 100;
            //float distance = MathF.Sqrt((x * x) + (y * y));

            //Debug.WriteLine($"distance:{l1} {l2} <=> {distance}");

            /*
            //return Vector3.DistanceSquared(l1, l2);
            float x = l1.X - l2.X;
            float y = l1.Y - l2.Y;
            //x = x * 100;
            //y = y * 100;
            //float distance = MathF.Sqrt((x * x) + (y * y));
            return MathF.Sqrt((x * x) + (y * y));
            */

            return distance;
        }

        public static List<Vector3> ShortenRouteFromLocation(Vector3 location, List<Vector3> pointsList)
        {
            var result = new List<Vector3>();

            var closestDistance = pointsList.Select(p => (point: p, distance: DistanceXYTo(location, p)))
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

        public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 AP = P - A;       //Vector from A to P
            Vector2 AB = B - A;       //Vector from A to B

            float magnitudeAB = AB.LengthSquared();     //Magnitude of AB vector (it's length squared)
            float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point

            if (distance < 0)     //Check if P projection is over vectorAB
            {
                return A;
            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + (AB * distance);
            }
        }


        public static Vector2 AsVector2(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }
    }
}
