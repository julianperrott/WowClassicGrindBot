using PatherPath.Graph;

namespace PathingAPI.Data
{
    public class SphereEventArgs
    {
        public Location Location { get; private set; }
        public int Colour { get; private set; }
        public string Name { get; private set; }

        public SphereEventArgs(string name, Location location, int colour)
        {
            this.Name = name;
            this.Location = location;
            this.Colour = colour;
        }

        public Vertex Vertex => Vertex.Create(Location.X, Location.Y, Location.Z + 1);
    }
}