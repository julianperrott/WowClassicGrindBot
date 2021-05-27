using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLib;

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
                if (!areas.ContainsKey(x.UIMapId))
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

        public WorldMapArea? Get(int uiMapId)
        {
            if (areas.TryGetValue(uiMapId, out var map))
            {
                return map;
            }

            return null;
        }
    }
}
