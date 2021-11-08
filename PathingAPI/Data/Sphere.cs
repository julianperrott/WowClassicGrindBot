using System.Numerics;

namespace PathingAPI.Data
{
    public class Sphere
    {
        public Sphere() { }

        public string Name { get; set; }
        public Vector3 Spot { get; set; }
        public int Colour { get; set; }
        public int MapId { get; set; }
    }
}