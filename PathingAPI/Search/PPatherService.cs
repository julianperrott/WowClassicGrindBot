using PathingAPI.WorldToMap;
using PatherPath.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using WowTriangles;

namespace PathingAPI
{
    public class PPatherService
    {
        private readonly List<WorldMapArea> worldMapAreas;
        private Search search { get; set; }
        private readonly PatherPath.Logger logger;
        private Action<Path> OnPathCreated;
        public Action<string> OnLog { get; set; }
        public Action OnReset { get; set; }
        public Action<ChunkAddedEventArgs> OnChunkAdded { get; set; }

        private Path lastPath;
        public bool HasInitialised = false;

        public PPatherService()
        {
            this.worldMapAreas = WorldMapArea.Read();
            logger = new PatherPath.Logger(Log);
        }

        public PPatherService(Action<string> onWrite)
        {
            this.worldMapAreas = WorldMapArea.Read();
            logger = new PatherPath.Logger(onWrite);
        }

        public void Log(string message)
        {
            try
            {
                Console.WriteLine(message);
                OnLog?.Invoke(message);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        public void Reset()
        {
            OnReset?.Invoke();
            this.search = null;
        }

        private bool Initialise(string continent)
        {
            if (search == null || continent != search.continent)
            {
                HasInitialised = true;
                PathGraph.SearchEnabled = false;
                search = new Search(continent, logger);
                search.PathGraph.triangleWorld.NotifyChunkAdded = (e) => OnChunkAdded?.Invoke(e);
                OnReset?.Invoke();
                return true;
            }
            return false;
        }
        public Location GetWorldLocation(int uiMapId, float v1, float v2)
        {
            var worldMapArea = worldMapAreas.Where(i => i.UIMapId == uiMapId).FirstOrDefault();
            var worldX = worldMapArea.ToWorldX(v2);
            var worldY = worldMapArea.ToWorldY(v1);

            Initialise(worldMapArea.Continent);

            var location = search.CreateLocation(worldX, worldY);
            location.Continent = worldMapArea.Continent;
            return location;
        }

        public WorldMapAreaSpot ToMapAreaSpot(float x, float y, float z, int mapHint)
        {
            var area = WorldMapArea.GetWorldMapArea(worldMapAreas, x, y, search.continent, mapHint);
            return new WorldMapAreaSpot
            {
                Y = area.ToMapX(x),
                X = area.ToMapY(y),
                Z = z,
                MapID = area.UIMapId
            };
        }

        public Path DoSearch(PathGraph.eSearchScoreSpot searchType)
        {
            var path = search.DoSearch(searchType);
            OnPathCreated?.Invoke(path);
            lastPath = path;
            return path;
        }

        public void SetOnPathCreated(Action<Path> action)
        {
            OnPathCreated = action;
            if (lastPath != null)
            {
                OnPathCreated?.Invoke(lastPath);
            }
        }

        public bool SetLocations(Location from, Location to)
        {
            this.Initialise(from.Continent);

            bool hasChanged = this.search.locationFrom == null || this.search.locationTo == null ||
                from.X != this.search.locationFrom.X ||
                from.Y != this.search.locationFrom.Y ||
                to.X != this.search.locationTo.X ||
                to.Y != this.search.locationTo.Y;

            this.search.locationFrom = from;
            this.search.locationTo = to;

            return hasChanged;
        }

        public List<TriangleCollection> SetNotifyChunkAdded(Action<ChunkAddedEventArgs> action)
        {
            OnChunkAdded = action;

            if (search == null)
            {
                return new List<TriangleCollection>();
            }

            return search.PathGraph.triangleWorld.LoadedChunks;
        }

        public List<Spot> GetCurrentSearchPath()
        {
            if (search == null || search.PathGraph == null)
            {
                return null;
            }

            return search.PathGraph.CurrentSearchPath();
        }
    }
}