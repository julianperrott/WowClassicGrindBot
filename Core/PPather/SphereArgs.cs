namespace Core.PPather
{
    public class SphereArgs
    {
        public string Name { get; set; } = string.Empty;
        public WowPoint Spot { get; set; } = new WowPoint(0, 0);
        public int Colour { get; set; }
        public int MapId { get; set; }
    }
}
