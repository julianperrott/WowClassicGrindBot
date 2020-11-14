using System;
using System.Collections.Generic;
using System.Text;

namespace Libs.PPather
{
    public class LineArgs
    {
        public string Name { get; set; } = string.Empty;
        public List<WowPoint> Spots { get; set; } = new List<WowPoint>();
        public int Colour { get; set; }
        public int MapId { get; set; }
    }
}
