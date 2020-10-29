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
    public class Path
    {
        public List<Location> locations { get; set; } = new List<Location>();

        public Path()
        {
        }

        public Path(List<Spot> steps)
        {
            foreach (Spot s in steps)
            {
                AddLast(s.location);
            }
        }

        public int Count()
        {
            return locations.Count;
        }

        public Location GetFirst()
        {
            return Get(0);
        }

        public Location GetSecond()
        {
            if (locations.Count > 1)
                return Get(1);
            return null;
        }

        public Location GetRandom()
        {
            if (locations.Count < 2)
                return null;
            Random r = new Random();
            return locations[r.Next(0, (locations.Count - 1))];
        }

        public Location GetLast()
        {
            return locations[locations.Count - 1];
        }

        public Location RemoveFirst()
        {
            Location l = Get(0);
            locations.RemoveAt(0);
            return l;
        }

        public Location Get(int index)
        {
            return locations[index];
        }

        public void AddFirst(Location l)
        {
            locations.Insert(0, l);
        }

        public void AddFirst(Path l)
        {
            locations.InsertRange(0, l.locations);
        }

        public void AddLast(Location l)
        {
            locations.Add(l);
        }

        public void AddLast(Path l)
        {
            locations.AddRange(l.locations);
        }
    }
}