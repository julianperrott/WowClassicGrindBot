using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Core.PPather;
using Core.Database;
using System.Threading;
using System.Diagnostics;
using AnTCP.Client;
using SharedLib;

namespace Core
{
    public sealed class RemotePathingAPIV3 : IPPather
    {
        public enum EMessageType
        {
            PATH,
            MOVE_ALONG_SURFACE,
            RANDOM_POINT,
            RANDOM_POINT_AROUND,
            CAST_RAY,
            RANDOM_PATH,
            PATH_LOCATIONS
        }

        private readonly ILogger logger;
        private readonly WorldMapAreaDB worldMapAreaDB;

        // TODO remove this
        private int watchdogPollMs = 1000;

        private List<LineArgs> lineArgs = new List<LineArgs>();

        private int targetMapId = 0;

        private AnTcpClient Client { get; }
        private Thread ConnectionWatchdog { get; }

        private bool ShouldExit { get; set; }

        public RemotePathingAPIV3(ILogger logger, string ip, int port, WorldMapAreaDB worldMapAreaDB)
        {
            this.logger = logger;
            this.worldMapAreaDB = worldMapAreaDB;

            Client = new AnTcpClient(ip, port);
            ConnectionWatchdog = new Thread(ObserveConnection);
            ConnectionWatchdog.Start();
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
            await Task.Delay(0);
            throw new NotImplementedException();
            //return new List<WowPoint>();
        }

        public async Task<List<WowPoint>> FindRouteTo(AddonReader addonReader, WowPoint destination)
        {
            int uiMapId = addonReader.UIMapId.Value;
            WowPoint fromPoint = addonReader.PlayerReader.PlayerLocation;
            WowPoint toPoint = destination;

            if (!Client.IsConnected)
            {
                return new List<WowPoint>();
            }

            if (targetMapId == 0)
            {
                targetMapId = uiMapId;
            }

            try
            {
                await Task.Delay(0);

                Vector3 start = worldMapAreaDB.GetWorldLocation(uiMapId, fromPoint, true);
                Vector3 end = worldMapAreaDB.GetWorldLocation(uiMapId, toPoint, true);

                var result = new List<WowPoint>();

                if (!worldMapAreaDB.TryGet(uiMapId, out WorldMapArea area))
                    return new List<WowPoint>();

                // incase haven't asked a pathfinder for a route this value will be 0
                // that case use the highest location
                if (start.Z == 0)
                {
                    start.Z = area.LocTop / 2;
                    end.Z = area.LocTop / 2;
                }

                logger.LogInformation($"Finding route from {fromPoint}({start}) map {uiMapId} to {toPoint}({end}) map {targetMapId}...");

                var path = Client.Send((byte)EMessageType.PATH_LOCATIONS, (area.MapID, start, end, 2)).AsArray<Vector3>();
                if (path == null)
                    return result;

                for (int i = 0; i < path.Length; i++)
                {
                    // Z X Y -> X Y Z
                    var p = worldMapAreaDB.ToMapAreaSpot(path[i].Z, path[i].X, path[i].Y, area.Continent, uiMapId);
                    result.Add(new WowPoint(p.X, p.Y, p.Z));
                    logger.LogInformation($"new float[] {{ {path[i].Z}f, {path[i].X}f, {path[i].Y}f }},");
                }

                if (result.Count > 0)
                {
                    addonReader.PlayerReader.ZCoord = result[0].Z;
                    logger.LogInformation($"PlayerLocation.Z = {addonReader.PlayerReader.PlayerLocation.Z}");
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


        public Task<bool> PingServer()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            var task = Task.Run(() =>
            {
                cts.CancelAfter(2 * watchdogPollMs);
                Stopwatch sw = Stopwatch.StartNew();

                while (!cts.IsCancellationRequested)
                {
                    if (Client.IsConnected)
                    {
                        break;
                    }
                }

                sw.Stop();

                logger.LogInformation($"{GetType().Name} PingServer {sw.ElapsedMilliseconds}ms {Client.IsConnected}");

                return Client.IsConnected;
            });

            return task;
        }

        public void RequestDisconnect()
        {
            ShouldExit = true;
            ConnectionWatchdog.Join();
        }

        #endregion old

        private void ObserveConnection()
        {
            while (!ShouldExit)
            {
                if (!Client.IsConnected)
                {
                    try
                    {
                        Client.Connect();
                    }
                    catch
                    {
                        // ignored, will happen when we cant connect
                    }
                }

                Thread.Sleep(watchdogPollMs);
            }
        }

    }
}