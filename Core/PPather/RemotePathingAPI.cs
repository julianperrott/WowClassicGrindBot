using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Core.PPather;
using System.Text;
using System.Numerics;

namespace Core
{
    public class RemotePathingAPI : IPPather
    {
        private readonly ILogger logger;

        private string host = "localhost";

        private int port = 5001;

        private string api => $"http://{host}:{port}/api/PPather/";

        private List<LineArgs> lineArgs = new List<LineArgs>();

        private int targetMapId;

        public RemotePathingAPI(ILogger logger, string host="", int port=0)
        {
            this.logger = logger;
            this.host = host;
            this.port = port;
        }

        public async ValueTask DrawLines(List<LineArgs> lineArgs)
        {
            this.lineArgs = lineArgs;

            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    using (var content = new StringContent(JsonConvert.SerializeObject(lineArgs), Encoding.UTF8, "application/json"))
                    {
                        logger.LogInformation($"Drawing lines '{string.Join(", ", lineArgs.Select(l => l.MapId))}'...");
                        await client.PostAsync($"{api}Drawlines", content);
                    }
                }
            }
        }

        public async ValueTask DrawLines()
        {
            await DrawLines(lineArgs);
        }

        public async ValueTask DrawSphere(SphereArgs args)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    using (var content = new StringContent(JsonConvert.SerializeObject(args), Encoding.UTF8, "application/json"))
                    {
                        await client.PostAsync($"{api}DrawSphere", content);
                    }
                }
            }
        }

        public async ValueTask<List<Vector3>> FindRoute(int map, Vector3 fromPoint, Vector3 toPoint)
        {
            targetMapId = map;

            try
            {
                logger.LogInformation($"Finding route from {fromPoint} map {map} to {toPoint} map {targetMapId}...");
                var url = $"{api}MapRoute?map1={map}&x1={fromPoint.X}&y1={fromPoint.Y}&map2={targetMapId}&x2={toPoint.X}&y2={toPoint.Y}";
                var sw = new Stopwatch();
                sw.Start();

                using (var handler = new HttpClientHandler())
                {
                    using (var client = new HttpClient(handler))
                    {
                        var responseString = await client.GetStringAsync(url);
                        logger.LogInformation($"Finding route from {fromPoint} map {map} to {toPoint} took {sw.ElapsedMilliseconds} ms.");
                        var path = JsonConvert.DeserializeObject<IEnumerable<WorldMapAreaSpot>>(responseString);
                        var result = path.Select(l => new Vector3(l.X, l.Y, 0)).ToList();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Finding route from {fromPoint} to {toPoint}");
                Console.WriteLine(ex);
                return new List<Vector3>();
            }
        }

        public async ValueTask<List<Vector3>> FindRouteTo(AddonReader addonReader, Vector3 destination)
        {
            return await FindRoute(addonReader.UIMapId.Value, addonReader.PlayerReader.PlayerLocation, destination);
        }

        public async Task<bool> PingServer()
        {
            try
            {
                var url = $"{api}SelfTest";

                using (var handler = new HttpClientHandler())
                {
                    using (var client = new HttpClient(handler))
                    {
                        var responseString = await client.GetStringAsync(url);
                        var result = JsonConvert.DeserializeObject<bool>(responseString);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"INFO: Pathing API is not running remotely, this means the local one will be used. {api} Gave({ex.Message}).");
                return false;
            }
        }
    }
}