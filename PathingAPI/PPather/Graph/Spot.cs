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

using System;
using System.Collections.Generic;

namespace PatherPath.Graph
{
    public class Spot
    {
        private Logger logger;

        public Spot(Logger logger)
        {
            this.logger = logger;
        }

        public const float Z_RESOLUTION = 2.0f; // Z spots max this close

        public const uint FLAG_VISITED = 0x0001;
        public const uint FLAG_BLOCKED = 0x0002;
        public const uint FLAG_MPQ_MAPPED = 0x0004;
        public const uint FLAG_WATER = 0x0008;
        public const uint FLAG_INDOORS = 0x0010;
        public const uint FLAG_CLOSETOMODEL = 0x0020;

        public float X, Y, Z;
        public uint flags;

        public int n_paths = 0;
        public float[] paths; // 3 floats per outgoing path

        public GraphChunk chunk = null;
        public Spot next;  // list on same x,y, used by chunk

        public int searchID = 0;
        public Spot traceBack; // Used by search
        public float traceBackDistance = 0; // Used by search
        public float score; // Used by search
        public bool closed, scoreSet;

        public Spot(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Spot(Location l)
        {
            this.X = l.X;
            this.Y = l.Y;
            this.Z = l.Z;
        }

        public bool IsCloseToModel()
        {
            return IsFlagSet(FLAG_CLOSETOMODEL);
        }

        public bool IsBlocked()
        {
            return IsFlagSet(FLAG_BLOCKED);
        }

        public bool IsInWater()
        {
            if (IsFlagSet(FLAG_WATER))
                return true;
            return false;
        }

        public Location location
        {
            get
            {
                return new Location(X, Y, Z);
            }
        }

        public float GetDistanceTo(Location l)
        {
            float dx = l.X - X;
            float dy = l.Y - Y;
            float dz = l.Z - Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float GetDistanceTo(Spot s)
        {
            float dx = s.X - X;
            float dy = s.Y - Y;
            float dz = s.Z - Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float GetDistanceTo2D(Spot s)
        {
            return MathF.Sqrt(MathF.Pow(X - s.X, 2.0f) + MathF.Pow(Y - s.Y, 2.0f));
        }

        public bool IsCloseZ(float z)
        {
            float dz = z - this.Z;
            if (dz > Z_RESOLUTION)
                return false;
            if (dz < -Z_RESOLUTION)
                return false;
            return true;
        }

        public void SetFlag(uint flag, bool val)
        {
            uint old = flags;
            if (val)
                flags |= flag;
            else
                flags &= ~flag;
            if (chunk != null && old != flags)
                chunk.modified = true;
        }

        public bool IsFlagSet(uint flag)
        {
            return (flags & flag) != 0;
        }

        public void SetLocation(Location l)
        {
            X = l.X;
            Y = l.Y;
            Z = l.Z;
            if (chunk != null)
                chunk.modified = true;
        }

        public Location GetLocation()
        {
            return new Location(X, Y, Z);
        }

        public override string ToString()
        {
            return GetLocation().ToString();
        }

        public bool GetPath(int i, out float x, out float y, out float z)
        {
            x = y = z = 0;
            if (i > n_paths)
                return false;
            int off = i * 3;
            x = paths[off];
            y = paths[off + 1];
            z = paths[off + 2];
            return true;
        }

        public Spot GetToSpot(PathGraph pg, int i)
        {
            float x, y, z;
            GetPath(i, out x, out y, out z);
            return pg.GetSpot(x, y, z);
        }

        public List<Spot> GetPathsToSpots(PathGraph pg)
        {
            List<Spot> list = new List<Spot>(n_paths);
            for (int i = 0; i < n_paths; i++)
            {
                Spot spot = GetToSpot(pg, i);
                if (spot != null) { list.Add(spot); }
            }
            return list;
        }

        public List<Location> GetPaths()
        {
            List<Location> l = new List<Location>();
            if (paths == null)
                return l;
            for (int i = 0; i < n_paths; i++)
            {
                int off = i * 3;
                Location loc = new Location(paths[off], paths[off + 1], paths[off + 2]);
                l.Add(loc);
            }
            return l;
        }

        public bool HasPathTo(PathGraph pg, Spot s)
        {
            for (int i = 0; i < n_paths; i++)
            {
                Spot to = GetToSpot(pg, i);
                if (to == s)
                    return true;
            }
            return false;
        }

        public bool HasPathTo(Location l)
        {
            return HasPathTo(l.X, l.Y, l.Z);
        }

        public bool HasPathTo(float x, float y, float z)
        {
            if (paths == null)
                return false;
            for (int i = 0; i < n_paths; i++)
            {
                int off = i * 3;
                if (x == paths[off] &&
                   y == paths[off + 1] &&
                   z == paths[off + 2])
                    return true;
            }
            return false;
        }

        public void AddPathTo(Spot s)
        {
            AddPathTo(s.X, s.Y, s.Z);
        }

        public void AddPathTo(Location l)
        {
            AddPathTo(l.X, l.Y, l.Z);
        }

        public void AddPathTo(float x, float y, float z)
        {
            if (HasPathTo(x, y, z))
                return;
            int old_size;
            if (paths == null)
                old_size = 0;
            else
                old_size = paths.Length / 3;
            if (n_paths + 1 > old_size)
            {
                int new_size = old_size * 2;
                if (new_size < 4)
                    new_size = 4;
                Array.Resize<float>(ref paths, new_size * 3);
            }

            int off = n_paths * 3;
            paths[off] = x;
            paths[off + 1] = y;
            paths[off + 2] = z;
            n_paths++;
            if (chunk != null)
                chunk.modified = true;
        }

        public void RemovePathTo(Location l)
        {
            RemovePathTo(l.X, l.Y, l.Z);
        }

        public void RemovePathTo(float x, float y, float z)
        {
            // look for it
            int found_index = -1;
            for (int i = 0; i < n_paths && found_index == -1; i++)
            {
                int off = i * 3;
                if (paths[off] == x &&
                   paths[off + 1] == y &&
                   paths[off + 2] == z)
                {
                    found_index = i;
                }
            }
            if (found_index != -1)
            {
                logger.Debug(string.Format("Remove path ({0}) to {1} {2} {3}", found_index, x, y, n_paths));
                for (int i = found_index; i < n_paths - 1; i++)
                {
                    int off = i * 3;
                    paths[off] = paths[off + 3];
                    paths[off + 1] = paths[off + 4];
                    paths[off + 2] = paths[off + 5];
                }
                n_paths--;
                if (chunk != null)
                    chunk.modified = true;
            }
            else
            {
                logger.Debug(string.Format("Found not path to remove ({0}) to {1} {2} ", found_index, x, y));
            }
        }

        // search stuff

        public bool SetSearchID(int id)
        {
            if (searchID != id)
            {
                closed = false;
                scoreSet = false;
                searchID = id;
                return true;
            }
            return false;
        }

        public bool SearchIsClosed(int id)
        {
            if (id == searchID)
                return closed;
            return false;
        }

        public void SearchClose(int id)
        {
            SetSearchID(id);
            closed = true;
        }

        public bool SearchScoreIsSet(int id)
        {
            if (id == searchID)
            {
                return scoreSet;
            }
            return false;
        }

        public float SearchScoreGet(int id)
        {
            if (id == searchID)
            {
                return score;
            }
            return float.MaxValue;
        }

        public void SearchScoreSet(int id, float score)
        {
            SetSearchID(id);
            this.score = score;
            scoreSet = true;
        }
    }
}