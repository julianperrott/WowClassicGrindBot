using Core;
using Core.PPather;
using Microsoft.Extensions.Logging;
using PathingAPI;
using System.Collections.Generic;
using System.Diagnostics;
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

        private int targetMapId = 0;

        public LocalPathingApi(ILogger logger, PPatherService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public async Task DrawLines(List<LineArgs> lineArgs)
        {
            await Task.Delay(0);
        }

        public async Task DrawLines()
        {
            await Task.Delay(0);
        }

        public async Task DrawSphere(SphereArgs args)
        {
            await Task.Delay(0);
        }

        public async Task<List<WowPoint>> FindRoute(int map, WowPoint fromPoint, WowPoint toPoint)
        {
            if (!Enabled)
            {
                logger.LogWarning($"Pathing is disabled, please check the messages when the bot started.");
                return new List<WowPoint>();
            }

            if (targetMapId == 0)
            {
                targetMapId = map;
            }

            await Task.Delay(0);

            var sw = new Stopwatch();
            sw.Start();

            service.SetLocations(service.GetWorldLocation(map, (float)fromPoint.X, (float)fromPoint.Y), service.GetWorldLocation(targetMapId, (float)toPoint.X, (float)toPoint.Y));
            var path = service.DoSearch(PatherPath.Graph.PathGraph.eSearchScoreSpot.A_Star_With_Model_Avoidance);

            if (path == null)
            {
                logger.LogWarning($"LocalPathingApi: Failed to find a path from {fromPoint} to {toPoint}");
                return new List<WowPoint>();
            }
            else
            {
                logger.LogInformation($"Finding route from {fromPoint} map {map} to {toPoint} took {sw.ElapsedMilliseconds} ms.");
                service.Save();
            }

            var worldLocations = path.locations.Select(s => service.ToMapAreaSpot(s.X, s.Y, s.Z, map));

            var result = worldLocations.Select(l => new WowPoint(l.X, l.Y)).ToList();
            return result;
        }

        public Task<List<WowPoint>> FindRouteTo(PlayerReader playerReader, WowPoint destination)
        {
            return FindRoute((int)playerReader.UIMapId.Value, playerReader.PlayerLocation, destination);
        }

        public bool SelfTest()
        {
            var mpqFiles = MPQTriangleSupplier.GetArchiveNames(DataConfig.Load(), s => logger.LogInformation(s));

            var countOfMPQFiles = mpqFiles.Where(f => File.Exists(f)).Count();
            //countOfMPQFiles = 0;

            if (countOfMPQFiles == 0)
            {
                logger.LogWarning("Some of these MPQ files should exist!");
                mpqFiles.ToList().ForEach(l => logger.LogInformation(l));
                logger.LogError("No MPQ files found, refer to the Readme to download them.");
                Enabled = false;
            }
            else
            {
                logger.LogDebug("Hooray, MPQ files exist.");
            }

            return countOfMPQFiles > 0;
        }
    }
}