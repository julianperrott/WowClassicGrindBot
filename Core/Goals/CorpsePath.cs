using System.Collections.Generic;

namespace Core.Goals
{
    public class CorpsePath
    {
        public WowPoint MyLocation { get; set; } = new WowPoint(0, 0);
        public WowPoint CorpseLocation { get; set; } = new WowPoint(0, 0);

        public List<WowPoint> RouteToCorpse { get; } = new List<WowPoint>();
        public List<WowPoint> TruncatedRoute { get; } = new List<WowPoint>();
    }
}