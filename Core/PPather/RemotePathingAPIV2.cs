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
using Core.Database;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Core
{
    public sealed class RemotePathingAPIV2 : CustomTcpClient, IPPather
    {
        private readonly ILogger logger;
        private readonly WorldMapAreaDB worldMapAreaDB;

        private List<LineArgs> lineArgs = new List<LineArgs>();

        private int targetMapId = 0;

        public RemotePathingAPIV2(ILogger logger, string ip, int port, WorldMapAreaDB worldMapAreaDB) : base(logger, ip, port)
        {
            this.logger = logger;
            this.worldMapAreaDB = worldMapAreaDB;
        }


        #region old

        public async Task DrawLines(List<LineArgs> lineArgs)
        {
            await Task.Delay(0);
        }

        public async Task DrawLines()
        {
            await DrawLines(lineArgs);
        }

        public async Task DrawSphere(SphereArgs args)
        {
            await Task.Delay(0);
        }

        public async Task<List<WowPoint>> FindRoute(int uiMapId, WowPoint fromPoint, WowPoint toPoint)
        {
            if (!IsConnected)
                return new List<WowPoint>();

            try
            {
                await Task.Delay(0);
                logger.LogInformation($"Finding route from {fromPoint} map {uiMapId} to {toPoint} map {targetMapId}...");

                Vector3 start = worldMapAreaDB.GetWorldLocation(uiMapId, fromPoint);
                Vector3 end = worldMapAreaDB.GetWorldLocation(uiMapId, toPoint);

                var area = worldMapAreaDB.Get(uiMapId);
                var request = new PathRequestWithLocationRequest(area.MapID, start, end, PathRequestFlags.None);

                int typeSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PathRequestWithLocationRequest));
                byte[]? response = SendData(request, typeSize);

                if(response == null || response.Length < typeSize)
                    return new List<WowPoint>();

                int vtypeSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
                Vector3[] path;
                int nodeCount = response.Length / vtypeSize;
                unsafe
                {
                    fixed (byte* pResult = response)
                    {
                        path = new Span<Vector3>(pResult, nodeCount).ToArray();
                    }
                }

                if (path == null)
                    return new List<WowPoint>();

                var result = new List<WowPoint>();

                for (int i=0; i<path.Length; i++)
                {
                    var p = worldMapAreaDB.ToMapAreaSpot(path[i].X, path[i].Y, path[i].Z, area.Continent, uiMapId);
                    // TODO: excusmewtf
                    result.Add(new WowPoint(p.Y, p.X));

                    logger.LogInformation($"new float[] {{ {path[i].X}f, {path[i].Y}f, {path[i].Z} }},");
                }

                return result;
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
            logger.LogInformation($"PingServer {2*watchdogPollMs}ms");
            await Task.Delay(2 * watchdogPollMs);
            return IsConnected;
        }

        #endregion old
    }
}