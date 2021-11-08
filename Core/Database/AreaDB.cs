using System;
using System.IO;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLib.Extensions;
using WowheadDB;

namespace Core.Database
{
    public class AreaDB
    {
        private readonly ILogger logger;
        private readonly DataConfig dataConfig;

        private int areaId = -1;
        public Area? CurrentArea { private set; get; }

        public AreaDB(ILogger logger, DataConfig dataConfig)
        {
            this.logger = logger;
            this.dataConfig = dataConfig;
        }

        public void Update(int areaId)
        {
            if (areaId > 0 && this.areaId != areaId)
            {
                try
                {
                    CurrentArea = JsonConvert.DeserializeObject<Area>(File.ReadAllText(Path.Join(dataConfig.Area, $"{areaId}.json")));
                }
                catch(Exception e)
                {
                    logger.LogError(e.Message, e.StackTrace);
                }
                this.areaId = areaId;
            }
        }
        
        public Vector3? GetNearestVendor(Vector3 playerLocation)
        {
            if (CurrentArea == null || CurrentArea.vendor.Count == 0)
                return null;

            NPC nearest = CurrentArea.vendor[0];
            float dist = playerLocation.DistanceTo(nearest.points[0]);

            CurrentArea.vendor.ForEach(npc =>
            {
                var d = playerLocation.DistanceTo(npc.points[0]);
                if (d < dist)
                {
                    dist = d;
                    nearest = npc;
                }
            });

            return nearest.points[0];
        }
    }
}
