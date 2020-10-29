using PatherPath.Graph;

namespace PathingAPI
{
    public class SearchParam
    {
        public string Continent { get; set; }
        public PathGraph.eSearchScoreSpot SearchType { get; set; }
        public Location From { get; set; }
        public Location To { get; set; }
    }
}
