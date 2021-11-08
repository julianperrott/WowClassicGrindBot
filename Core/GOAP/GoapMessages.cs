using System.Numerics;

namespace Core.GOAP
{
    public class CorpseLocation
    {
        public Vector3 WowPoint { get; private set; }
        public double Radius { get; private set; }

        public CorpseLocation(Vector3 wowPoint, double radius)
        {
            WowPoint = wowPoint;
            Radius = radius;
        }
    }
}
