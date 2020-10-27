using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Libs
{
    public class RemotePathingAPI : IPPather
    {
        private readonly ILogger logger;

        private string api = $"http://localhost:5001/api/PPather/";

        public RemotePathingAPI(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<List<WowPoint>> FindRoute(int map, WowPoint fromPoint, WowPoint toPoint)
        {
            try
            {
                //logger.LogInformation($"Finding route to {toPoint}");
                var url = $"{api}MapRoute?map1={map}&x1={fromPoint.X}&y1={fromPoint.Y}&map2={map}&x2={toPoint.X}&y2={toPoint.Y}";
                var sw = new Stopwatch();
                sw.Start();

                using (var handler = new HttpClientHandler())
                {
                    using (var client = new HttpClient(handler))
                    {
                        var responseString = await client.GetStringAsync(url);
                        logger.LogInformation($"Finding route from {fromPoint} map {map} to {toPoint} took {sw.ElapsedMilliseconds} ms.");
                        var path = JsonConvert.DeserializeObject<IEnumerable<WorldMapAreaSpot>>(responseString);
                        var result = path.Select(l => new WowPoint(l.X, l.Y)).ToList();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Finding route from {fromPoint} to {toPoint}");
                Console.WriteLine(ex);
                return new List<WowPoint>();
            }
        }

        public Task<List<WowPoint>> FindRouteTo(PlayerReader playerReader, WowPoint destination)
        {
            return FindRoute(playerReader.ZoneId, playerReader.PlayerLocation, destination);
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
                logger.LogInformation($"INFO: Pathing API is not running remotely, this means the local one will be used. {api} Gave({ex.Message}).");
                return false;
            }
        }
    }
}