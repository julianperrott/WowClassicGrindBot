using Core;
using Core.PPather;
using Microsoft.Extensions.Logging;
using PathingAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WowTriangles;

namespace BlazorServer
{
    public class LocalPathingApi : IPPather
    {
        private readonly ILogger logger;

        private PPatherService service;

        private bool Enabled = true;

        private int targetMapId;

        public LocalPathingApi(ILogger logger, PPatherService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public async Task DrawLines(List<LineArgs> lineArgs)
        {
            await ValueTask.CompletedTask;
        }

        public async Task DrawLines()
        {
            await ValueTask.CompletedTask;
        }

        public async Task DrawSphere(SphereArgs args)
        {
            await ValueTask.CompletedTask;
        }

        public async Task<List<Vector3>> FindRoute(int map, Vector3 fromPoint, Vector3 toPoint)
        {
            await ValueTask.CompletedTask;

            if (!Enabled)
            {
                logger.LogWarning($"Pathing is disabled, please check the messages when the bot started.");
                return new List<Vector3>();
            }

            targetMapId = map;

            var sw = new Stopwatch();
            sw.Start();

            service.SetLocations(service.GetWorldLocation(map, fromPoint.X, fromPoint.Y, fromPoint.Z), service.GetWorldLocation(targetMapId, toPoint.X, toPoint.Y));
            var path = service.DoSearch(PatherPath.Graph.PathGraph.eSearchScoreSpot.A_Star_With_Model_Avoidance);

            if (path == null)
            {
                logger.LogWarning($"[{nameof(LocalPathingApi)}]: Failed to find a path from {fromPoint} to {toPoint}");
                return new List<Vector3>();
            }
            else
            {
                logger.LogInformation($"[{nameof(LocalPathingApi)}]: Finding route from {fromPoint} map {map} to {toPoint} took {sw.ElapsedMilliseconds} ms.");
                service.Save();
            }

            var worldLocations = path.locations.Select(s => service.ToMapAreaSpot(s.X, s.Y, s.Z, map));
            var result = worldLocations.Select(l => new Vector3(l.X, l.Y, l.Z)).ToList();
            return result;
        }

        public Task<List<Vector3>> FindRouteTo(AddonReader addonReader, Vector3 destination)
        {
            return FindRoute(addonReader.UIMapId.Value, addonReader.PlayerReader.PlayerLocation, destination);
        }

        public bool SelfTest()
        {
            var mpqFiles = MPQTriangleSupplier.GetArchiveNames(DataConfig.Load(), s => logger.LogInformation(s));

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
                logger.LogDebug("Hooray, MPQ files exist.");
            }

            return countOfMPQFiles > 0;
        }
    }
}