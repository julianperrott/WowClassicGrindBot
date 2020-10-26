/*
  This file is part of ppather.

    PPather is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    PPather is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with ppather.  If not, see <http://www.gnu.org/licenses/>.

*/

using Common;
using Newtonsoft.Json;
using System;

namespace PatherPath.Graph
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Location : ILocation
    {
        [JsonProperty]
        private float x;

        [JsonProperty]
        private float y;

        [JsonProperty]
        private float z;

        [JsonIgnore]
        private string description;

        public string ToPatherString()
        {
            return string.Format("[{0},{1},{2}]", (int)X, (int)Y, (int)Z);
        }

        [JsonIgnore]
        public string Description { get { return description; } }

        [JsonIgnore]
        public string Continent { get; set; }

        public Location(ILocation l)
        {
            this.x = l.X;
            this.y = l.Y;
            this.z = l.Z;
        }

        public override bool Equals(object ob)
        {
            if (ob.GetType() == typeof(Location))
            {
                Location l = (Location)ob;
                return l.X == X && l.Y == Y && l.Z == Z;
            }
            return base.Equals(ob);
        }

        public Location(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Location(float x, float y, float z, string description, string continent)
            : this(x, y, z)
        {
            this.description = description + " ";
            this.Continent = continent;
        }

        public float X
        {
            get
            {
                return x;
            }
        }

        public float Y
        {
            get
            {
                return y;
            }
        }

        public float Z
        {
            get
            {
                return z;
            }
        }

        public float GetDistanceTo(ILocation l)
        {
            if (l == null) { return 999; }
            float dx = x - l.X;
            float dy = y - l.Y;
            float dz = z - l.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float GetDistanceTo2D(ILocation l)
        {
            float dx = x - l.X;
            float dy = y - l.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override String ToString()
        {
            String s = Continent + ":" + description + "[" + (int)x + "," + (int)y + "," + (int)z + "]";
            return s;
        }

        public Location InFrontOf(float heading, float d)
        {
            float nx = x + (float)Math.Cos(heading) * d;
            float ny = y + (float)Math.Sin(heading) * d;
            float nz = z;
            return new Location(nx, ny, nz);
        }
    }
}