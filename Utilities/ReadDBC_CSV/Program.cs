using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ReadDBC_CSV
{
    class Program
    {
        public class Spell
        {
            public int Id { get; set; }

            public override string ToString()
            {
                return Id.ToString();
            }

            public static List<string> columnIndexs = new List<string> { 
                "ID", "NameSubtext_lang", "Description_lang", "AuraDescription_lang"
            };

            public static List<Spell> Extract(string srcFile, string descLang)
            {
                var entryIndex = FindIndex(columnIndexs, "ID");
                var descIndex = FindIndex(columnIndexs, "Description_lang");

                var items = new List<Spell>();
                Action<string> extractLine = line =>
                {
                    var values = line.Split(",");
                    if(values.Length > entryIndex && values.Length > descIndex &&
                        values[descIndex].Contains(descLang))
                    {
                        Console.WriteLine($"{values[entryIndex]}");

                        items.Add(new Spell
                        {
                            Id = int.Parse(values[entryIndex])
                        });
                    }
                };

                ExtractItemTemplate(srcFile, extractLine);

                Console.WriteLine($"Spells\n{string.Join("\n", items)}\n");

                return items;
            }
        }

        public class Consumable
        {
            public int Id { get; set; }

            public override string ToString()
            {
                return Id.ToString();
            }

            public static List<string> columnIndexs = new List<string> {
                "ID", "LegacySlotIndex", "TriggerType", "Charges", 
                "CoolDownMSec", "CategoryCoolDownMSec",
                "SpellCategoryID", "SpellID", 
                "ChrSpecializationID", "ParentItemID" 
            };

            public static void Extract(string srcFile, List<Spell> spells, string destFile)
            {
                var entryIndex = FindIndex(columnIndexs, "ID");
                var spellIdIndex = FindIndex(columnIndexs, "SpellID");
                var ParentItemIDIndex = FindIndex(columnIndexs, "ParentItemID");

                var items = new List<Consumable>();
                Action<string> extractLine = line =>
                {
                    var values = line.Split(",");
                    if (values.Length > entryIndex && 
                        values.Length > spellIdIndex &&
                        values.Length > ParentItemIDIndex)
                    {
                        int spellId = int.Parse(values[spellIdIndex]);
                        if(spells.Any(s => s.Id == spellId))
                        {
                            int ItemId = int.Parse(values[ParentItemIDIndex]);
                            items.Add(new Consumable
                            {
                                Id = ItemId
                            });
                        }
                    }
                };

                ExtractItemTemplate(srcFile, extractLine);

                items.Sort((a, b) => a.Id.CompareTo(b.Id));
                Console.WriteLine($"Consumables\n{string.Join("\n", items)}\n");

                File.WriteAllText(destFile, JsonConvert.SerializeObject(items));
            }
        }

        static void Main(string[] args)
        {
            GenerateConsumables();
        }

        private static void GenerateConsumables()
        {
            var path = "../../../dbc/";

            var spell = Path.Join(path, "spell.csv");
            var foods = Spell.Extract(spell, "Restores $o1 health over $d");
            var waters = Spell.Extract(spell, "Restores $o1 mana over $d");

            var itemEffect = Path.Join(path, "itemeffect.csv");
            Consumable.Extract(itemEffect, foods, Path.Join(path, "foods.json"));
            Consumable.Extract(itemEffect, waters, Path.Join(path, "waters.json"));
        }

        private static int FindIndex(List<string> columnIndexs, string v)
        {
            for (int i = 0; i < columnIndexs.Count; i++)
            {
                if (columnIndexs[i] == v)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(v);
        }

        private static void ExtractItemTemplate(string file, Action<string> extractLine)
        {
            var stream = File.OpenText(file);

            // header
            var line = stream.ReadLine();
            line = stream.ReadLine();

            while (line != null)
            {
                extractLine(line);
                line = stream.ReadLine();
            }
        }
    }
}
