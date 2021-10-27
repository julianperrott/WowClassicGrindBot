using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharedLib;

namespace ReadDBC_CSV
{
    public class ConsumablesExtractor : IExtractor
    {
        private readonly string path;

        private readonly string foodDesc;
        private readonly string waterDesc;

        public List<string> FileRequirement { get; } = new List<string>
        {
            "spell.csv",
            "itemeffect.csv",
        };

        public ConsumablesExtractor(string path, string foodDesc, string waterDesc)
        {
            this.path = path;

            this.foodDesc = foodDesc;
            this.waterDesc = waterDesc;
        }

        public void Run()
        {
            var spell = Path.Join(path, FileRequirement[0]);

            var foodSpells = ExtractSpells(spell, foodDesc);
            var waterSpells = ExtractSpells(spell, waterDesc);

            var itemEffect = Path.Join(path, FileRequirement[1]);

            var foodIds = ExtractItem(itemEffect, foodSpells);
            foodIds.Sort((a, b) => a.Id.CompareTo(b.Id));
            Console.WriteLine($"Foods: {foodIds.Count}");
            File.WriteAllText(Path.Join(path, "foods.json"), JsonConvert.SerializeObject(foodIds));

            var waterIds = ExtractItem(itemEffect, waterSpells);
            waterIds.Sort((a, b) => a.Id.CompareTo(b.Id));
            Console.WriteLine($"Waters: {foodIds.Count}");
            File.WriteAllText(Path.Join(path, "waters.json"), JsonConvert.SerializeObject(waterIds));
        }

        private List<EntityId> ExtractSpells(string path, string descLang)
        {
            int entryIndex = -1;
            int descIndex = -1;

            var extractor = new CSVExtractor();
            extractor.HeaderAction = () =>
            {
                entryIndex = extractor.FindIndex("ID");
                descIndex = extractor.FindIndex("Description_lang");
            };

            var items = new List<EntityId>();
            Action<string> extractLine = line =>
            {
                var values = line.Split(",");
                if (values.Length > entryIndex &&
                    values.Length > descIndex &&
                    values[descIndex].Contains(descLang))
                {
                    items.Add(new EntityId
                    {
                        Id = int.Parse(values[entryIndex])
                    });
                }
            };

            extractor.ExtractTemplate(path, extractLine);
            return items;
        }

        private List<EntityId> ExtractItem(string path, List<EntityId> spells)
        {
            int entryIndex = -1;
            int spellIdIndex = -1;
            int ParentItemIDIndex = -1;

            var extractor = new CSVExtractor();
            extractor.HeaderAction = () =>
            {
                entryIndex = extractor.FindIndex("ID");
                spellIdIndex = extractor.FindIndex("SpellID");
                ParentItemIDIndex = extractor.FindIndex("ParentItemID");
            };

            var items = new List<EntityId>();
            Action<string> extractLine = line =>
            {
                var values = line.Split(",");
                if (values.Length > entryIndex &&
                    values.Length > spellIdIndex &&
                    values.Length > ParentItemIDIndex)
                {
                    int spellId = int.Parse(values[spellIdIndex]);
                    if (spells.Any(s => s.Id == spellId))
                    {
                        int ItemId = int.Parse(values[ParentItemIDIndex]);
                        items.Add(new EntityId
                        {
                            Id = ItemId
                        });
                    }
                }
            };

            extractor.ExtractTemplate(path, extractLine);

            return items;
        }

    }
}
