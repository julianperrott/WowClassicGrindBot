using PatherPath.Graph;
using System.Collections.Generic;
using System.Linq;

namespace PathingAPI.Data
{
    public class LinesEventArgs
    {
        public List<Location> Locations { get; private set; }
        public int Colour { get; private set; }
        public string Name { get; private set; }

        public LinesEventArgs(string name, List<Location> locations, int colour)
        {
            this.Name = name;
            this.Locations = locations;
            this.Colour = colour;
        }

        public IEnumerable<Vertex> Lines => Locations.Where(s => s != null).Select(s => Vertex.Create(s.X, s.Y, s.Z+5));
    }
}