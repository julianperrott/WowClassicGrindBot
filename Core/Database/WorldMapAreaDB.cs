using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLib;
using SharedLib.Data;

namespace Core.Database
{
    public class WorldMapAreaDB
    {
        private readonly ILogger logger;
        private Dictionary<int, WorldMapArea> areas = new Dictionary<int, WorldMapArea>();

        public WorldMapAreaDB(ILogger logger, DataConfig dataConfig)
        {
            this.logger = logger;

            var list = JsonConvert.DeserializeObject<List<WorldMapArea>>(File.ReadAllText(Path.Join(dataConfig.WorldToMap, "WorldMapArea.json")));
            list.ForEach(x =>
            {
                areas.Add(x.UIMapId, x);
            });
        }

        public int GetAreaId(int uiMapId)
        {
            if(areas.TryGetValue(uiMapId, out var map))
            {
                return map.AreaID;
            }

            return -1;
        }

        public Vector3 GetWorldLocation(int uiMapId, WowPoint p, bool flipXY)
        {
            var worldMapArea = areas[uiMapId];
            if(flipXY)
            {
                return new Vector3(worldMapArea.ToWorldX((float)p.Y), worldMapArea.ToWorldY((float)p.X), (float)p.Z);
            }
            else
            {
                var worldX = worldMapArea.ToWorldX((float)p.X);
                var worldY = worldMapArea.ToWorldY((float)p.Y);
                return new Vector3(worldX, worldY, (float)p.Z);
            }
        }

        public WorldMapAreaSpot ToMapAreaSpot(float x, float y, float z, string continent, int mapHint)
        {
            var area = WorldMapAreaFactory.GetWorldMapArea(areas.Values.ToList(), x, y, continent, mapHint);
            return new WorldMapAreaSpot
            {
                Y = area.ToMapX(x),
                X = area.ToMapY(y),
                Z = z,
                MapID = area.UIMapId
            };
        }
        
        public bool TryGet(int uiMapId, out WorldMapArea area)
        {
            if (areas.TryGetValue(uiMapId, out var map))
            {
                area = map;
                return true;
            }

            area = default;
            return false;
        }
    }
}
