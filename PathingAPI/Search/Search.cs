using PatherPath;
using PatherPath.Graph;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WowTriangles;

namespace PathingAPI
{
    public class Search
    {
        public PathGraph PathGraph { get; set; }
        public string continent;

        private Logger logger;

        public Location locationFrom { get; set; }
        public Location locationTo { get; set; }

        private const float toonHeight = 2.0f;
        private const float toonSize = 0.5f;


        public Search(string continent, Logger logger)
        {
            this.logger = logger;
            this.continent = continent;
            if (PathGraph == null)
            {
                CreatePathGraph(continent);
            }
        }

        public Location CreateLocation(float x, float y)
        {
            float z0,z1;
            int flags;
            if (PathGraph.triangleWorld.FindStandableAt1(x, y, -1000, 2000, out z0, out flags, toonHeight, toonSize, true))
            {
                // try to find a standable just under where we are just in case we are on top of a building.
                if (PathGraph.triangleWorld.FindStandableAt1(x, y, -1000, z0 - toonHeight - 1, out z1, out flags, toonHeight, toonSize, true))
                {
                    z0 = z1;
                }
            }
            return new Location(x, y, z0-toonHeight, "", continent);
        }

        public void CreatePathGraph(string continent)
        {
            MPQTriangleSupplier mpq = new MPQTriangleSupplier(this.logger);
            mpq.SetContinent(continent);
            var triangleWorld = new ChunkedTriangleCollection(512, this.logger);
            triangleWorld.SetMaxCached(512);
            triangleWorld.AddSupplier(mpq);
            PathGraph = new PathGraph(continent, triangleWorld, null, this.logger);
            this.continent = continent;
        }

        public Path DoSearch(PathGraph.eSearchScoreSpot searchType)
        {
            //create a new path graph if required
            if (PathGraph == null || this.continent != locationFrom.Continent)
            {
                CreatePathGraph(locationFrom.Continent);
            }

            PatherPath.Graph.PathGraph.SearchEnabled = true;

            // tell the pathgraph which type of search to do
            PathGraph.searchScoreSpot = searchType;

            //slow down the search if required.
            PathGraph.sleepMSBetweenSpots = 0;

            var path= PathGraph.CreatePath(locationFrom, locationTo, 5, null);

            return path;
        }
    }
}