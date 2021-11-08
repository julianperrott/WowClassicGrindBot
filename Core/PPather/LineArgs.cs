using System.Numerics;
using System.Collections.Generic;

namespace Core.PPather
{
    public class LineArgs
    {
        public string Name { get; set; } = string.Empty;
        public List<Vector3> Spots { get; set; } = new List<Vector3>();
        public int Colour { get; set; }
        public int MapId { get; set; }
    }
}
