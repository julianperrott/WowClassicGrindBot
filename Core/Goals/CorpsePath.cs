using System.Numerics;
using System.Collections.Generic;

namespace Core.Goals
{
    public class CorpsePath
    {
        public Vector3 MyLocation { get; set; }
        public Vector3 CorpseLocation { get; set; }

        public List<Vector3> RouteToCorpse { get; } = new List<Vector3>();
        public List<Vector3> TruncatedRoute { get; } = new List<Vector3>();
    }
}