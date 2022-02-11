using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using SharedLib;

namespace ReadDBC_CSV
{
    public class WorldMapAreaExtractor : IExtractor
    {
        private readonly string path;

        public List<string> FileRequirement { get; } = new List<string>()
        {
            "uimap.csv",
            "uimapassignment.csv",
            "map.csv"
        };

        public WorldMapAreaExtractor(string path)
        {
            this.path = path;
        }

        public void Run()
        {
            // UIMapId - AreaName
            var uimap = Path.Join(path, FileRequirement[0]);
            var wmas = ExtractUIMap(uimap);

            // MapID - AreaID - LocBottom - LocRight - LocTop - LocLeft
            var uimapassignment = Path.Join(path, FileRequirement[1]);
            ExtractBoundaries(uimapassignment, wmas);

            // Continent / Directory
            var map = Path.Join(path, FileRequirement[2]);
            ExtractContinent(map, wmas);

            ClearEmptyBound(wmas);

            Console.WriteLine($"WMAs: {wmas.Count}");
            File.WriteAllText(Path.Join(path, "WorldMapArea.json"), JsonConvert.SerializeObject(wmas, Formatting.Indented));
        }

        private static List<WorldMapArea> ExtractUIMap(string path)
        {
            int idIndex = -1;
            int nameIndex = -1;

            var extractor = new CSVExtractor();
            extractor.HeaderAction = () =>
            {
                idIndex = extractor.FindIndex("ID");
                nameIndex = extractor.FindIndex("Name_lang");
            };

            var items = new List<WorldMapArea>();
            Action<string> extractLine = line =>
            {
                var values = line.Split(",");
                if (values.Length > idIndex &&
                    values.Length > nameIndex)
                {
                    int uiMapId = int.Parse(values[idIndex]);
                    items.Add(new WorldMapArea
                    {
                        UIMapId = uiMapId,
                        AreaName = values[nameIndex]
                    });
                }
            };

            extractor.ExtractTemplate(path, extractLine);
            return items;
        }

        private static void ExtractBoundaries(string path, List<WorldMapArea> wmas)
        {
            int uiMapIdIndex = -1;
            int mapIdIndex = -1;
            int areaIdIndex = -1;

            int orderIndexIndex = -1;

            int region0Index = -1;
            int region1Index = -1;
            int region3Index = -1;
            int region4Index = -1;

            var extractor = new CSVExtractor();
            extractor.HeaderAction = () =>
            {
                uiMapIdIndex = extractor.FindIndex("UiMapID");
                mapIdIndex = extractor.FindIndex("MapID");
                areaIdIndex = extractor.FindIndex("AreaID");

                orderIndexIndex = extractor.FindIndex("OrderIndex");

                region0Index = extractor.FindIndex("Region[0]");
                region1Index = extractor.FindIndex("Region[1]");

                region3Index = extractor.FindIndex("Region[3]");
                region4Index = extractor.FindIndex("Region[4]");
            };

            Action<string> extractLine = line =>
            {
                var values = line.Split(",");
                if (values.Length > uiMapIdIndex &&
                    values.Length > mapIdIndex &&
                    values.Length > areaIdIndex &&

                    values.Length > region0Index &&
                    values.Length > region1Index &&
                    values.Length > region3Index &&
                    values.Length > region4Index
                    )
                {
                    int uiMapId = int.Parse(values[uiMapIdIndex]);
                    int orderIndex = int.Parse(values[orderIndexIndex]);

                    int index = wmas.FindIndex(x => x.UIMapId == uiMapId && orderIndex == 0);
                    if (index > -1)
                    {
                        var wma = wmas[index];
                        wmas[index] = new WorldMapArea
                        {
                            MapID = int.Parse(values[mapIdIndex]),
                            AreaID = int.Parse(values[areaIdIndex]),

                            AreaName = wma.AreaName,

                            LocBottom = float.Parse(values[region0Index]),
                            LocRight = float.Parse(values[region1Index]),

                            LocTop = float.Parse(values[region3Index]),
                            LocLeft = float.Parse(values[region4Index]),

                            UIMapId = wma.UIMapId,
                            Continent = wma.Continent,
                        };
                    }
                }
            };

            extractor.ExtractTemplate(path, extractLine);
        }

        private static void ExtractContinent(string path, List<WorldMapArea> wmas)
        {
            int mapIdIndex = -1;
            int directoryIndex = -1;

            var extractor = new CSVExtractor();
            extractor.HeaderAction = () =>
            {
                mapIdIndex = extractor.FindIndex("ID");
                directoryIndex = extractor.FindIndex("Directory");
            };

            Action<string> extractLine = line =>
            {
                string[] values;
                if (line.Contains('\"'))
                    values = CSVExtractor.SplitQuotes(line);
                else
                    values = line.Split(",");

                if (values.Length > directoryIndex &&
                    values.Length > mapIdIndex)
                {
                    int mapId = int.Parse(values[mapIdIndex]);

                    var list = wmas.FindAll(x => x.MapID == mapId);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var wma = list[i];
                        list[i] = new WorldMapArea
                        {
                            MapID = wma.MapID,
                            AreaID = wma.AreaID,

                            AreaName = wma.AreaName,

                            LocBottom = wma.LocBottom,
                            LocRight = wma.LocRight,

                            LocTop = wma.LocTop,
                            LocLeft = wma.LocLeft,

                            UIMapId = wma.UIMapId,
                            Continent = values[directoryIndex]
                        };
                    }
                }
            };

            extractor.ExtractTemplate(path, extractLine);
        }

        private static void ClearEmptyBound(List<WorldMapArea> wmas)
        {
            for (int i = wmas.Count - 1; i >= 0; i--)
            {
                if (wmas[i].LocBottom == 0 &&
                    wmas[i].LocLeft == 0 &&
                    wmas[i].LocRight == 0 &&
                    wmas[i].LocTop == 0)
                {
                    wmas.RemoveAt(i);
                }
            }
        }
    }
}
