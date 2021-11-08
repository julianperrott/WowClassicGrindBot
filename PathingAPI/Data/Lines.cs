using System.Collections.Generic;
using System.Numerics;

namespace PathingAPI.Data
{
    public class Lines
    {
        public Lines(){}

        public string Name { get; set; }
        public List<Vector3> Spots { get; set; }
        public int Colour { get; set; }
        public int MapId { get; set; }
    }
}