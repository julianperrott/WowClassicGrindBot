using System.Collections.Generic;
using System;

namespace WowheadDB
{
    public class NPC
    {
        public List<List<double>> coords;

        public List<WowPoint> points { get => WowPoint.FromList(coords); } 

        public int level;
        public string name;
        public int type;
        public int id;
        public int reacthorde;
        public int reactalliance;
        public string description;
    }
}