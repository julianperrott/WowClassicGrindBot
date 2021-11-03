using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        
        public WowPoint? GetNearestVendor(WowPoint playerLocation)
        {
            if (CurrentArea == null || CurrentArea.vendor.Count == 0)
                return null;

            NPC nearest = CurrentArea.vendor[0];
            double dist = WowPoint.DistanceTo(nearest.points[0], playerLocation);

            CurrentArea.vendor.ForEach(npc =>
            {
                var d = WowPoint.DistanceTo(npc.points[0], playerLocation);
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
