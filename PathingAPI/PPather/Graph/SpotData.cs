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

using System.Collections.Generic;

namespace PatherPath.Graph
{
    internal class SpotData<T>
    {
        private Dictionary<Spot, T> data = new Dictionary<Spot, T>();

        public T Get(Spot s)
        {
            T t = default(T);
            data.TryGetValue(s, out t);
            return t;
        }

        public void Set(Spot s, T t)
        {
            if (data.ContainsKey(s))
                data.Remove(s);
            data.Add(s, t);
        }

        public bool IsSet(Spot s)
        {
            return data.ContainsKey(s);
        }
    }
}