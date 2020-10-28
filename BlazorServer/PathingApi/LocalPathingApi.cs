using Libs;
using Microsoft.Extensions.Logging;
using PathingAPI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WowTriangles;

namespace BlazorServer
{
    public class LocalPathingApi : IPPather
    {
        private readonly ILogger logger;

        private PPatherService service;

        private bool Enabled = true;

        public LocalPathingApi(ILogger logger, PPatherService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public async Task<List<WowPoint>> FindRoute(int map, WowPoint fromPoint, WowPoint toPoint)
        {
            if (!Enabled)
            {
                logger.LogWarning($"Pathing is disabled, please check the messages when the bot started.");
                return new List<WowPoint>();
            }

            await Task.Delay(0);

            service.SetLocations(service.GetWorldLocation(map, (float)fromPoint.X, (float)fromPoint.Y), service.GetWorldLocation(map, (float)toPoint.X, (float)toPoint.Y));
            var path = service.DoSearch(PatherPath.Graph.PathGraph.eSearchScoreSpot.A_Star);

            if (path == null)
            {
                logger.LogWarning($"LocalPathingApi: Failed to find a path from {fromPoint} to {toPoint}");
                return new List<WowPoint>();
            }

            var worldLocations = path.locations.Select(s => service.ToMapAreaSpot(s.X, s.Y, s.Z, map));

            var result = worldLocations.Select(l => new WowPoint(l.X, l.Y)).ToList();
            return result;
        }

        public Task<List<WowPoint>> FindRouteTo(PlayerReader playerReader, WowPoint destination)
        {
            return FindRoute((int)playerReader.ZoneId, playerReader.PlayerLocation, destination);
        }

        public bool SelfTest()
        {
            var mpqFiles = MPQTriangleSupplier.GetArchiveNames(s=>logger.LogInformation(s));

            var countOfMPQFiles = mpqFiles.Where(f => File.Exists(f)).Count();

            if (countOfMPQFiles == 0)
            {
                logger.LogWarning("Some of these MPQ files should exist!");
                mpqFiles.ToList().ForEach(l => logger.LogInformation(l));
                logger.LogError("No MPQ files found, refer to the Readme to download them.");
                Enabled = false;
            }
            else
            {
                logger.LogInformation("Hooray, MPQ files exist.");
            }

            return countOfMPQFiles > 0;
        }
    }
}