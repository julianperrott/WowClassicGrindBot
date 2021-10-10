namespace Core.GOAP
{
    public class CorpseLocation
    {
        public WowPoint WowPoint { get; private set; }
        public double Radius { get; private set; }

        public CorpseLocation(WowPoint wowPoint, double radius)
        {
            WowPoint = wowPoint;
            Radius = radius;
        }
    }
}
