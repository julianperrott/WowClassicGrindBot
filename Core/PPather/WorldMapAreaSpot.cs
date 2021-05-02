
using Newtonsoft.Json;

namespace Core
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
