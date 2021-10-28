using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SharedLib;

namespace ReadDBC_CSV
{
    public class ItemExtractor : IExtractor
    {
        private readonly string path;

        public List<string> FileRequirement { get; } = new List<string>()
        {
            "itemsparse.csv"
        };

        public ItemExtractor(string path)
        {
            this.path = path;
        }

        public void Run()
        {
            var itemsearchname = Path.Join(path, FileRequirement[0]);
            var items = ExtractItems(itemsearchname);

            Console.WriteLine($"Items: {items.Count}");
            File.WriteAllText(Path.Join(path, "items.json"), JsonConvert.SerializeObject(items));

        }

        private List<Item> ExtractItems(string path)
        {
            int idIndex = -1;
            int nameIndex = -1;
            int qualityIndex = -1;
            int sellPriceIndex = -1;

            var extractor = new CSVExtractor();
            extractor.HeaderAction = () =>
            {
                idIndex = extractor.FindIndex("ID");
                nameIndex = extractor.FindIndex("Display_lang");
                qualityIndex = extractor.FindIndex("OverallQualityID");
                sellPriceIndex = extractor.FindIndex("SellPrice");
            };

            var items = new List<Item>();
            Action<string> extractLine = line =>
            {
                string[] values = line.Split(",");
                if (line.Contains("\""))
                    values = extractor.SplitQuotes(line);
                else
                    values = line.Split(",");

                if (values.Length > idIndex &&
                    values.Length > nameIndex &&
                    values.Length > qualityIndex &&
                    values.Length > sellPriceIndex)
                {
                    items.Add(new Item
                    {
                        Entry = int.Parse(values[idIndex]),
                        Quality = int.Parse(values[qualityIndex]),
                        Name = values[nameIndex],
                        SellPrice = int.Parse(values[sellPriceIndex])
                    });
                }
            };

            extractor.ExtractTemplate(path, extractLine);
            return items;
        }
    }
}
