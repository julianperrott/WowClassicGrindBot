using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharedLib.Data
{
    public static class WorldMapAreaFactory
    {
        public static List<WorldMapArea> Read(ILogger logger, DataConfig dataConfig)
        {
            return JsonConvert.DeserializeObject<List<WorldMapArea>>(File.ReadAllText(Path.Join(dataConfig.WorldToMap, "WorldMapArea.json")));
        }

        public static WorldMapArea GetWorldMapArea(List<WorldMapArea> worldMapAreas, float x, float y, string continent, int uiMapIdHint)
        {
            var maps = worldMapAreas
                .Where(i => x <= i.LocTop)
                .Where(i => x >= i.LocBottom)
                .Where(i => y <= i.LocLeft)
                .Where(i => y >= i.LocRight)
                .Where(i => i.Continent == continent)
                .ToList();

            if (!maps.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(worldMapAreas), $"Failed to find map area for spot {x}, {y}, {continent}, {uiMapIdHint}");
            }

            if (maps.Count > 1)
            {
                // sometimes we end up with 2 map areas which a coord could be in which is rather unhelpful. e.g. Silithus and Feralas overlap.
                // If we are in a zone and not moving between then the mapHint should take care of the issue
                // otherwise we are not going to be able to work out which zone we are actually in...

                if (uiMapIdHint > 0)
                {
                    return maps.First(m => m.UIMapId == uiMapIdHint);
                }

                throw new ArgumentOutOfRangeException(nameof(worldMapAreas), $"Found many map areas for spot {x}, {y}, {continent}, {uiMapIdHint} : {string.Join(", ", maps.Select(s => s.AreaName))}");
            }

            return maps.First();
        }

    }
}
