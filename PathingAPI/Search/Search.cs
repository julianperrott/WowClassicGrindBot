using PatherPath;
using PatherPath.Graph;
using System;
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
            // find model 0 i.e. terrain
            var z0 = GetZValueAt(x, y, new int[] { 0 });

            // if no z value found then try any model
            if (z0 == float.MinValue) { z0 = GetZValueAt(x, y, null); }

            if (z0 == float.MinValue) { z0 = 0; }

            return new Location(x, y, z0 - toonHeight, "", continent);
        }

        private float GetZValueAt(float x, float y, int[] allowedModels)
        {
            float z0 = float.MinValue, z1;
            int flags;

            if (allowedModels != null)
            {
                PathGraph.triangleWorld.FindStandableAt1(x, y, -1000, 2000, out z1, out flags, toonHeight, toonSize, true, null);
            }

            if (PathGraph.triangleWorld.FindStandableAt1(x, y, -1000, 2000, out z1, out flags, toonHeight, toonSize, true, allowedModels))
            {
                z0 = z1;
                // try to find a standable just under where we are just in case we are on top of a building.
                if (PathGraph.triangleWorld.FindStandableAt1(x, y, -1000, z0 - toonHeight - 1, out z1, out flags, toonHeight, toonSize, true, allowedModels))
                {
                    z0 = z1;
                }
            }
            else
            {
                return float.MinValue;
            }

            return z0;
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

            try
            {
                return PathGraph.CreatePath(locationFrom, locationTo, 5, null);
            }
            catch(Exception ex)
            {
                logger.WriteLine(ex.Message);
                return null;
            }
        }
    }
}