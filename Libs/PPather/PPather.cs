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
    public class PPather : IPPather
    {
        private readonly PlayerReader playerReader;
        private readonly ILogger logger;

        private string api = $"http://localhost:5001/api/PPather/MapRoute";

        public PPather(PlayerReader playerReader, ILogger logger)
        {
            this.playerReader = playerReader;
            this.logger = logger;
        }

        public async Task<List<WowPoint>> FindRoute(long map, WowPoint fromPoint, WowPoint toPoint)
        {
            try
            {
                //logger.LogInformation($"Finding route to {toPoint}");
                var url = $"{api}?map1={map}&x1={fromPoint.X}&y1={fromPoint.Y}&map2={map}&x2={toPoint.X}&y2={toPoint.Y}";
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

        public Task<List<WowPoint>> FindRouteTo(WowPoint destination)
        {
            return FindRoute(this.playerReader.ZoneId, this.playerReader.PlayerLocation, destination);
        }
    }
}