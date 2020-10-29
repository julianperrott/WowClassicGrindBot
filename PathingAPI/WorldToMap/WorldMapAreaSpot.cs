using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PathingAPI.WorldToMap
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WorldMapAreaSpot
    {
        [JsonProperty]
        public float X { get; set; }

        [JsonProperty]
        public float Y { get; set; }

        [JsonProperty]
        public float Z { get; set; }

        [JsonProperty]
        public int MapID { get; set; }
    }
}
