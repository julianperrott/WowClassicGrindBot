using System.Collections.Generic;
using System.IO;
using System.Numerics;
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

        public Vector3 GetWorldLocation(int uiMapId, Vector3 p, bool flipXY)
        {
            var worldMapArea = areas[uiMapId];
            if (flipXY)
            {
                return new Vector3(worldMapArea.ToWorldX(p.Y), worldMapArea.ToWorldY(p.X), p.Z);
            }
            else
            {
                return new Vector3(worldMapArea.ToWorldX(p.X), worldMapArea.ToWorldY(p.Y), p.Z);
            }
        }

        public WorldMapAreaSpot ToMapAreaSpot(float x, float y, float z, string continent, int mapHint)
        {
            var area = WorldMapAreaFactory.GetWorldMapArea(areas.Values, x, y, continent, mapHint);
            return new WorldMapAreaSpot
            {
                X = area.ToMapY(y),
                Y = area.ToMapX(x),
                Z = z,
                MapID = area.UIMapId
            };
        }
        public Vector3 ToAreaLoc(float x, float y, float z, string continent, int mapHint)
        {
            var area = WorldMapAreaFactory.GetWorldMapArea(areas.Values, x, y, continent, mapHint);
            return new Vector3(area.ToMapY(y), area.ToMapX(x), z);
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
