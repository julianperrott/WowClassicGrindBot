using System.Collections.Generic;
using System.Numerics;
using SharedLib.Extensions;

namespace WowheadDB
{
    public class NPC
    {
        public List<List<float>> coords;

        public int level;
        public string name;
        public int type;
        public int id;
        public int reacthorde;
        public int reactalliance;
        public string description;

        public List<Vector3> points => VectorExt.FromList(coords);
    }
}