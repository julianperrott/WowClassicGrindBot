using System.Collections.Generic;

namespace PathingAPI.Data
{
    public class Lines
    {
        public Lines(){}

        public string Name { get; set; }
        public List<WowPoint> Spots { get; set; }
        public int Colour { get; set; }
        public int MapId { get; set; }
    }
}