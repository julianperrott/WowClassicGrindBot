namespace PathingAPI.Data
{
    public class Vertex
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public int flags { get; set; }

        public static Vertex Create(float x, float y, float z, int flags)
        {
            return new Vertex
            {
                flags = flags,
                x = x / 10,
                y = y / 10,
                z = z / 10
            };
        }

        public static Vertex Create(float x, float y, float z)
        {
            return Create(x, y, z, 0);
        }
    }
}