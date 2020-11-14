using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#nullable enable

namespace PathingAPI
{
    public class WorldMapArea
    {
        public int ID { get; set; }
        public int MapID { get; set; }
        public int AreaID { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public float LocLeft { get; set; }
        public float LocRight { get; set; }
        public float LocTop { get; set; }
        public float LocBottom { get; set; }
        public int UIMapId { get; set; }
        public string Continent { get; set; } = string.Empty;

        public float ToWorldX(float value)
        {
            return ((LocBottom - LocTop) * value / 100) + LocTop;
        }

        public float ToWorldY(float value)
        {
            return ((LocRight - LocLeft) * value / 100) + LocLeft;
        }

        public float ToMapX(float value)
        {
            return 100-(((value - LocBottom) * 100) / (LocTop- LocBottom));
        }

        public float ToMapY(float value)
        {
            return 100-(((value - LocRight) * 100) / (LocLeft- LocRight));
        }

        public static WorldMapArea CreateV2(string[] values)
        {
            //https://wow.tools/dbc/?dbc=worldmaparea&build=2.0.0.5610#page=1&search=eas
            return new WorldMapArea
            {
                ID = int.Parse(values[0]),
                MapID = int.Parse(values[1]),
                AreaID = int.Parse(values[2]),
                AreaName = values[3],
                LocLeft = float.Parse(values[4]),
                LocRight = float.Parse(values[5]),
                LocTop = float.Parse(values[6]),
                LocBottom = float.Parse(values[7]),
            };
        }
        public static WorldMapArea CreateV1(string[] values)
        {
            //https://wow.tools/dbc/?dbc=worldmaparea&build=1.13.0.28377
            return new WorldMapArea
            {
                AreaName = values[0],
                LocLeft = float.Parse(values[1]),
                LocRight = float.Parse(values[2]),
                LocTop = float.Parse(values[3]),
                LocBottom = float.Parse(values[4]),

                MapID = int.Parse(values[6]),
                AreaID = int.Parse(values[7]),

                ID = int.Parse(values[15]),

            };
        }

        public static List<WorldMapArea> Read(PatherPath.Logger logger)
        {
            //CreateWorldMapAreaJson();

            var list = JsonConvert.DeserializeObject<List<WorldMapArea>>(File.ReadAllText(@"..\PathingAPI\WorldToMap\WorldMapArea.json"));
            logger.WriteLine("Unsupported mini maps areas: " + string.Join(", ", list.Where(l => l.UIMapId == 0).Select(s => s.AreaName).OrderBy(s => s)));

            return list;
        }

        private static List<WorldMapArea> CreateWorldMapAreaJson()
        {
            var list = File.ReadAllLines(@"..\PathingAPI\WorldToMap\WorldMapArea.csv").ToList().Skip(1).Select(l => l.Split(","))
                .Select(l => CreateV1(l))
                .ToList();

            var uimapLines = File.ReadAllLines(@"..\PathingAPI\WorldToMap\uimap.csv").ToList().Select(l => l.Split(","));
            list.ForEach(wmp => PopulateUIMap(wmp, uimapLines));
            System.IO.File.WriteAllText(@"..\PathingAPI\WorldToMap\WorldMapArea2.json", JsonConvert.SerializeObject(list));
            return list;
        }

        private static void PopulateUIMap(WorldMapArea area,IEnumerable<string[]> uimapLines)
        {
            var kalidor= uimapLines.Where(s=>s[0]== "Kalimdor").Select(s=>s[1]).FirstOrDefault();

            var matches = uimapLines.Where(s => Matches(area, s))
                .ToList();

            if (matches.Count > 1)
            {

            }

            if (matches.Count == 0)
            {

            }

            matches.ForEach(a =>
                {
                    area.UIMapId = int.Parse(a[1]);
                    area.Continent = a[2] == kalidor ? "Kalimdor" : "Azeroth";
                });
        }

        /// <summary>
        /// Fix occurance where Stormwind and Stormwind city didn't match
        /// </summary>
        /// <param name="area"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool Matches(WorldMapArea area, string[] s)
        {
            var areaname = s[0].Replace(" ", "").Replace("'", "");
            return areaname.StartsWith(area.AreaName, StringComparison.InvariantCultureIgnoreCase)
                 || area.AreaName.StartsWith(areaname, StringComparison.InvariantCultureIgnoreCase);
        }

        public static WorldMapArea? GetWorldMapArea(List<WorldMapArea> worldMapAreas, float x, float y, string continent, int mapHint)
        {
            var maps = worldMapAreas.Where(i => x <= i.LocTop)
                .Where(i => x >= i.LocBottom)
                .Where(i => y <= i.LocLeft)
                .Where(i => y >= i.LocRight)
                .Where(i => i.AreaName != "Azeroth")
                .Where(i => i.AreaName != "Kalimdor")
                .Where(i => i.Continent == continent)
                .ToList();

            if (!maps.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(worldMapAreas), $"Failed to find map area for spot {x}, {y}");
            }

            if (maps.Count > 1)
            {
                // sometimes we end up with 2 map areas which a coord could be in which is rather unhelpful. e.g. Silithus and Feralas overlap.
                // If we are in a zone and not moving between then the mapHint should take care of the issue
                // otherwise we are not going to be able to work out which zone we are actually in...

                if (mapHint > 0)
                {
                    var map = maps.Where(m => m.UIMapId == mapHint).FirstOrDefault();
                    if (map != null)
                    {
                        return map;
                    }
                }

                throw new ArgumentOutOfRangeException(nameof(worldMapAreas), "Found many map areas for spot {x}, {y}: {string.Join(", ", maps.Select(s => s.AreaName))}");
            }

            return maps.First();
        }
    }
}
